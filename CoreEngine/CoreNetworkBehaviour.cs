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

        // 위버가 코드를 안전하게 찔러넣을 수 있는 공간이면서 외부 접근은 완벽히 차단
        protected virtual void Awake()
        {
            // 컴파일 시 FishNet Weaver가 여기에 NetworkInitialize___Early() 등을 몰래 주입할 수 있음
        }

        protected virtual void OnDestroy()
        {
            // 컴파일 시 FishNet Weaver가 여기에 네트워크 메모리 해제 로직을 몰래 주입할 수 있음
        }


        protected virtual void OnEnable()
        {
            // 잠재적 Null 오류 원천 차단: 네트워크 뼈대가 조립되기 전이라면 무시
            if (base.NetworkObject == null) return;
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