using CoreEngine.EventBus;
using CoreEngine.Network.FishNetExtension.Extensions;
using FishNet.Connection;
using FishNet.Object;

namespace CoreEngine.Network.FishNetExtension
{
    public enum NetworkTickTarget
    {
        None,
        ServerOnly,
        ClientOnly,
        OwnerOnly,
        ServerAndClient
    }

    /// <summary>
    /// FishNet 네트워크 생명주기와 Tick 자동 등록/해제를 통제하는 뼈대 클래스
    /// </summary>
    public abstract class CoreNetworkBehaviour : NetworkBehaviour
    {
        protected abstract NetworkTickTarget networkTickTarget { get; }

        // ref로 넘기기 위해 인스턴스가 쥐고 있는 상태값
        private bool _isRegistered = false;

        protected virtual void OnEnable()
        {
            if (base.IsSpawned) this.TryRegisterNetworkTick(ref _isRegistered, networkTickTarget);
        }

        protected virtual void OnDisable() => this.TryUnregisterNetworkTick(ref _isRegistered);

        public override void OnStartServer() => this.TryRegisterNetworkTick(ref _isRegistered, networkTickTarget);
        public override void OnStopServer() => this.TryUnregisterNetworkTick(ref _isRegistered);

        public override void OnStartClient() => this.TryRegisterNetworkTick(ref _isRegistered, networkTickTarget);
        public override void OnStopClient() => this.TryUnregisterNetworkTick(ref _isRegistered);

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            this.TryUnregisterNetworkTick(ref _isRegistered);
            this.TryRegisterNetworkTick(ref _isRegistered, networkTickTarget);
        }
    }
}