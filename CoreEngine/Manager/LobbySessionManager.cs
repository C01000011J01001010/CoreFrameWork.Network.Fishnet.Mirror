using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Observing;
using CoreEngine.EventBus;
using CoreEngine.Network.Lobby;

namespace CoreEngine.Network.FishNetExtension.Lobby
{
    public struct LobbyPlayerData
    {
        public string IpAddress;
        public int EntryOrder; // 화면에 배치될 절대 순서
    }

    [RequireComponent(typeof(NetworkObserver))] // 항상 보여야하니
    public class LobbySessionManager : BaseNetworkManager
    {
        // 4명에게서 받은 데이터를 저장하고, 모든 클라이언트에게 자동으로 뿌려주기 위해 SyncDictionary 사용
        
        private readonly SyncDictionary<int, LobbyPlayerData> _connectedClients = new();

        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // SyncDictionary가 변경될 때마다 로컬 EventBus를 통해 UI에 핑을 날려 화면을 갱신
            _connectedClients.OnChange += OnClientsDictionaryChanged;
            _connectedClients.Clear(); // 안전하게 한번 더 호출
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // 클라이언트도 데이터를 동기화 받으니 같이 정리해야됨
            // 연결을 끊기 전에 호출
            _connectedClients.Clear();
            _connectedClients.OnChange -= OnClientsDictionaryChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // 서버가 열리면 접속/해제 이벤트를 수신하여 딕셔너리 관리
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
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
            string finalIp = string.IsNullOrEmpty(ipAddress) ? "Localhost" : ipAddress;

            // 고유 자리 번호를 부여하고 카운터를 증가
            _connectedClients[conn.ClientId] = new LobbyPlayerData
            {
                IpAddress = finalIp,
                EntryOrder = GetAvailableEntryOrder()
            };

            //_connectedClients[conn.ClientId] = string.IsNullOrEmpty(ipAddress) ? "Localhost" : ipAddress;
        }

        private int GetAvailableEntryOrder()
        {
            for (int i = 0; i < 4; i++) // 최대 4인방 기준
            {
                bool isOccupied = false;
                foreach (var kvp in _connectedClients)
                {
                    if (kvp.Value.EntryOrder == i)
                    {
                        isOccupied = true;
                        break;
                    }
                }
                if (!isOccupied) return i;
            }
            return 0;
        }

        // SyncDictionary 동기화 콜백
        private void OnClientsDictionaryChanged(SyncDictionaryOperation op, int key, LobbyPlayerData value, bool asServer)
        {
            // 방장(Host)일 때, 클라이언트 ID가 아직 발급되지 않은 시점의 섣부른 서버 콜백을 무시
            if (asServer && !IsServerOnlyStarted) return;

            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    EventBus<LobbyClientUpdateEvent>.Publish(new LobbyClientUpdateEvent
                    {
                        ClientId = key,
                        EntryOrder = value.EntryOrder, // UI 정렬용 데이터 추가
                        IpAddress = value.IpAddress,
                        clientUpdate = ClientUpdate.Add
                    });
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