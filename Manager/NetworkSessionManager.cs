using CoreEngine.EventBus;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace CoreEngine.Network
{
    public class NetworkSessionManager : BaseManager
    {
        // 마지막 접속 상태를 모니터링
        [SerializeField] private LocalConnectionState _lastClientState;
        [SerializeField] private LocalConnectionState _lastServerState;

        protected override void OnEnable()
        {
            base.OnEnable();

            // UI의 접속 요청 대기
            EventBus<ConnectRequestEvent>.Subscribe(OnConnectRequest);

            // FishNet의 비동기 연결 상태 콜백 구독
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.OnClientConnectionState += OnClientStateChanged;

            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnServerConnectionState += OnServerStateChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<ConnectRequestEvent>.Unsubscribe(OnConnectRequest);

            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.OnClientConnectionState -= OnClientStateChanged;

            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnServerConnectionState -= OnServerStateChanged;
        }

        private void OnConnectRequest(ConnectRequestEvent evt)
        {
            // 통신 배달부(Tugboat)의 설정값을 런타임에 제어
            if (InstanceFinder.TransportManager.Transport is Tugboat tugboat)
            {
                tugboat.SetPort(evt.Port);
                if (evt.Mode == ConnectionMode.Client || evt.Mode == ConnectionMode.Host)
                {
                    tugboat.SetClientAddress(evt.IpAddress);
                }
            }

            // 요청된 모드에 따라 접속 시도
            switch (evt.Mode)
            {
                case ConnectionMode.Host:
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection();
                    break;
                case ConnectionMode.Server:
                    InstanceFinder.ServerManager.StartConnection();
                    break;
                case ConnectionMode.Client:
                    InstanceFinder.ClientManager.StartConnection();
                    break;
            }
        }

        // 클라이언트 콜백 수신부
        private void OnClientStateChanged(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                EventBus<NetworkConnectionSuccessEvent>.Publish(new NetworkConnectionSuccessEvent());
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                switch(_lastClientState)
                {
                    case LocalConnectionState.Starting:
                        string meg = "호스트를 찾을 수 없거나\n타임아웃 되었습니다.";
                        EventBus<NetworkConnectionFailEvent>.Publish(new NetworkConnectionFailEvent { ErrorMessage = meg }); 
                        break;
                    case LocalConnectionState.Stopping:
                        EventBus< NetworkConnectionLostEvent >.Publish(new NetworkConnectionLostEvent());
                        break;
                }
                    
            }

            _lastClientState = args.ConnectionState;
        }

        // 서버 콜백 수신부
        private void OnServerStateChanged(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                EventBus<NetworkConnectionSuccessEvent>.Publish(new NetworkConnectionSuccessEvent());
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                switch (_lastServerState)
                {
                    case LocalConnectionState.Starting:
                        string meg = "서버 개설에 실패했습니다.";
                        EventBus<NetworkConnectionFailEvent>.Publish(new NetworkConnectionFailEvent { ErrorMessage = meg });
                        break;
                    case LocalConnectionState.Stopping:
                        EventBus<NetworkConnectionLostEvent>.Publish(new NetworkConnectionLostEvent());
                        break;
                }
            }

            _lastServerState = args.ConnectionState;
        }
    }
}