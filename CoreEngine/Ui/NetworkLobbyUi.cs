using CoreEngine.EventBus;
using CoreEngine.Facades;
using CoreEngine.Network.Pool;
using CoreEngine.SceneManagement;
using FishNet;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoreEngine.Network.Lobby.Ui
{
    public class NetworkLobbyUi : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private Button gameStartButton;
        [SerializeField] private Button exitRoomButton;

        [Header("--- Settings ---")]
        [SerializeField] private Color myProfileColor = Color.green;
        [SerializeField] private SceneReference NextScene;

        // 활성화된 클라이언트 딕셔너리와 풀(Pool) 큐
        private Dictionary<int, NetworkLobbyClientBox> _activeItems = new Dictionary<int, NetworkLobbyClientBox>();

        private void Awake()
        {
            EventBus<NetworkConnectionSuccessEvent>.Subscribe(OnNetworkConnectionSuccess);
            EventBus<LobbyClientUpdateEvent>.Subscribe(OnClientUpdateReceived);

            gameStartButton.onClick.AddListener(OnGameStartClicked);
            exitRoomButton.onClick.AddListener(OnExitLobbyClicked);

            gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            EventBus<NetworkConnectionSuccessEvent>.Unsubscribe(OnNetworkConnectionSuccess);
            EventBus<LobbyClientUpdateEvent>.Unsubscribe(OnClientUpdateReceived);

            gameStartButton.onClick.RemoveListener(OnGameStartClicked);
            exitRoomButton.onClick.RemoveListener(OnExitLobbyClicked);
        }

        private void OnNetworkConnectionSuccess(NetworkConnectionSuccessEvent evt)
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            // Server-Only 모드이거나 Host일 경우에만 게임 시작 버튼 활성화
            gameStartButton.gameObject.SetActive(InstanceFinder.IsServerStarted);
        }

        private void OnDisable()
        {
            ClearAllClientItems();
        }

        private void OnClientUpdateReceived(LobbyClientUpdateEvent evt)
        {
            switch (evt.clientUpdate)
            {
                case ClientUpdate.Add: AddClientToScrollView(evt.ClientId, evt.IpAddress); break;
                case ClientUpdate.Remove: RemoveClientFromScrollView(evt.ClientId); break;
                case ClientUpdate.Clear: ClearAllClientItems(); break;
            }
        }

        private void AddClientToScrollView(int clientId, string ip)
        {
            if (_activeItems.ContainsKey(clientId)) return;

            // Facade를 통해 NetworkPoolManager 구체 클래스로 접근[cite: 1]
            var poolManager = CoreFacade.GetManager<NetworkPoolManager>();
            if (poolManager == null) return;

            // 풀에서 스폰 후 Content 하위로 이동 (UI 크기 왜곡 방지를 위해 SetParent의 두 번째 인자를 false로 설정)
            GameObject obj = poolManager.Spawn(NetworkPoolType.LobbyClientBox, Vector3.zero);
            obj.transform.SetParent(contentParent, false);

            if (obj.TryGetComponent(out NetworkLobbyClientBox item))
            {
                bool isLocal = false;

                // 호스트(방장) 환경: 지연되는 ClientManager 대신 ServerManager의 팩트 체크를 활용
                if (InstanceFinder.IsServerStarted)
                {
                    // 서버는 이 클라이언트를 이미 승인하여 딕셔너리에 넣었습니다. 
                    // 서버가 가진 커넥션 정보를 꺼내와서, 그것이 내 로컬(방장) 커넥션인지 완벽하게 판별합니다.
                    if (InstanceFinder.ServerManager.Clients.TryGetValue(clientId, out var conn))
                    {
                        isLocal = conn.ClientId == 0;
                    }
                }
                // 참가자(Client) 환경: 기존 로직 정상 작동 (클라이언트는 이미 IsClientStarted = true 인 상태로 씬에 들어옴)
                else
                {
                    if (InstanceFinder.ClientManager.Connection != null)
                    {
                        isLocal = (InstanceFinder.ClientManager.Connection.ClientId == clientId);
                    }
                }

                item.Setup(clientId, ip, isLocal, myProfileColor);
                _activeItems.Add(clientId, item);
            }
        }

        private void RemoveClientFromScrollView(int clientId)
        {
            if (_activeItems.TryGetValue(clientId, out NetworkLobbyClientBox item))
            {
                // 3. Manager를 거치지 않고 객체 스스로 엔진 내장 반환 로직을 호출하게 위임[cite: 1]
                item.ReturnToPool();

                _activeItems.Remove(clientId);
            }
        }

        private void OnGameStartClicked()
        {
            if(!string.IsNullOrEmpty(NextScene))
            {
                // 방장이 버튼을 누르면 NetworkSceneFlowDirector가 가로채어 참가자들까지 강제 동기화하여 씬을 전환하도록 EventBus 발행
                EventBus<SceneLoadRequestEvent>.Publish(new SceneLoadRequestEvent(NextScene));
            }
            gameObject.SetActive(false);
        }

        private void OnExitLobbyClicked()
        {
            // 방을 나가면 현재 연결을 끊음. 
            // 연결이 끊어지면 FishNet 콜백(Stopped)에 의해 이전 네트워크 연결 UI가 자동으로 활성화되도록 구조화 가능
            if (InstanceFinder.IsServerStarted) InstanceFinder.ServerManager.StopConnection(true);
            if (InstanceFinder.IsClientStarted) InstanceFinder.ClientManager.StopConnection();

            gameObject.SetActive(false);
        }

        private void ClearAllClientItems()
        {
            foreach (var kvp in _activeItems)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ReturnToPool();
                }
            }

            _activeItems.Clear();
        }
    }
}