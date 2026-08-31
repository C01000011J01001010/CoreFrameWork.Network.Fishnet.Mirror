using CoreEngine.EventBus;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Observing;
using UnityEngine;

namespace CoreEngine.Network.Lobby
{
    [RequireComponent(typeof(NetworkObserver))] // 항상 보여야하니
    public class LobbySessionManager : NetworkBehaviour
    {
        // 4명에게서 받은 데이터를 저장하고, 모든 클라이언트에게 자동으로 뿌려주기 위해 SyncDictionary 사용
        
        private readonly SyncDictionary<int, string> _connectedClients = new SyncDictionary<int, string>();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // SyncDictionary가 변경될 때마다 로컬 EventBus를 통해 UI에 핑을 날려 화면을 갱신
            _connectedClients.OnChange += OnClientsDictionaryChanged;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _connectedClients.OnChange -= OnClientsDictionaryChanged;
        }

        public override void OnStartServer()
        {
            // 이전 Host에 의한 강제 종료시 찌꺼기 데이터를 완벽히 백지화
            _connectedClients.Clear();
            // 서버가 열리면 접속/해제 이벤트를 수신하여 딕셔너리 관리
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void OnRemoteConnectionState(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
            {
                AddClientToLobby(conn);
            }
            else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
            {
                _connectedClients.Remove(conn.ClientId);
            }
        }

        private void AddClientToLobby(NetworkConnection conn)
        {
            // FishNet Connection에서 클라이언트 IP 주소를 추출하여 저장
            string ipAddress = conn.GetAddress();

            _connectedClients[conn.ClientId] = string.IsNullOrEmpty(ipAddress) ? "Localhost" : ipAddress;
        }

        // SyncDictionary 동기화 콜백
        private void OnClientsDictionaryChanged(SyncDictionaryOperation op, int key, string value, bool asServer)
        {
            // 방장(Host)일 때, 클라이언트 ID가 아직 발급되지 않은 시점의 섣부른 서버 콜백을 무시
            if (asServer && !IsServerOnlyStarted) return;

            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    EventBus<LobbyClientUpdateEvent>.
                        Publish(new LobbyClientUpdateEvent { ClientId = key, IpAddress = value, clientUpdate = ClientUpdate.Add });
                    break;
                case SyncDictionaryOperation.Remove:
                    EventBus<LobbyClientUpdateEvent>.Publish(new LobbyClientUpdateEvent { ClientId = key, clientUpdate = ClientUpdate.Remove });
                    break;
                case SyncDictionaryOperation.Clear:
                    // 방이 백지화될 때 기존 UI 프리팹들을 일괄 풀(Pool) 반납 처리하는 이벤트 발송
                    EventBus<LobbyClientUpdateEvent>.Publish(new LobbyClientUpdateEvent { clientUpdate = ClientUpdate.Clear });
                    break;
            }
        }
    }
}