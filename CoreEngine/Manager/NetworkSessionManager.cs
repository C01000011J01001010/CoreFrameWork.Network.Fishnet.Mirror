using CoreEngine.EventBus;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Manager
{
    public class NetworkSessionManager : BaseManager
    {
        // 마지막 접속 상태를 모니터링
        [SerializeField] private LocalConnectionState _lastClientState;
        [SerializeField] private LocalConnectionState _lastServerState;

        private bool _isClientConnecting;
        private bool _isServerStarting;

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

        

        private void OnClientStateChanged(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Starting:
                    _isClientConnecting = true;
                    break;

                case LocalConnectionState.Started:
                    _isClientConnecting = false;
                    EventBus<NetworkConnectionSuccessEvent>.Publish(new NetworkConnectionSuccessEvent());
                    break;

                case LocalConnectionState.Stopped:
                    if (_isClientConnecting)
                    {
                        // 연결 시도 중 실패
                        _isClientConnecting = false;
                        const string message = "호스트를 찾을 수 없거나\n타임아웃 되었습니다.";
                        EventBus<NetworkConnectionFailEvent>.Publish(new NetworkConnectionFailEvent{ErrorMessage = message});
                    }
                    else
                    {
                        // 이미 연결된 상태에서 연결이 끊김
                        EventBus<NetworkConnectionLostEvent>.Publish(new NetworkConnectionLostEvent());
                    }
                    break;
            }

            _lastClientState = args.ConnectionState;
        }


        // 서버 콜백 수신부
        private void OnServerStateChanged(ServerConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Starting:
                    _isServerStarting = true;
                    break;

                case LocalConnectionState.Started:
                    _isServerStarting = false;
                    EventBus<NetworkConnectionSuccessEvent>.Publish(new NetworkConnectionSuccessEvent());
                    break;

                case LocalConnectionState.Stopped:
                    if (_isServerStarting)
                    {
                        // 서버 개설 실패
                        _isServerStarting = false;
                        const string message = "서버 개설에 실패했습니다.";
                        EventBus<NetworkConnectionFailEvent>.Publish(new NetworkConnectionFailEvent{ ErrorMessage = message});
                    }
                    else
                    {
                        // 정상적으로 실행된 서버가 종료됨
                        EventBus<NetworkConnectionLostEvent>.Publish(new NetworkConnectionLostEvent());
                    }
                    break;
            }

            _lastServerState = args.ConnectionState;
        }
    }
}