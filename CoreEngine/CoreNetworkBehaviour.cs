using CoreEngine.EventBus;
using CoreEngine.Extensions;
using CoreEngine.Facades;
using CoreEngine.Network.FishNetExtension.Extensions;
using CoreEngine.Pool;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using System.Collections;

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
    public abstract class CoreNetworkBehaviour : NetworkBehaviour, ISpawnable
    {
        protected abstract NetworkTickTarget networkTickTarget { get; }

        // ref로 넘기기 위해 인스턴스가 쥐고 있는 상태값
        private bool _isRegistered = false;

        // 초기화 상태 모니터링 프로퍼티
        protected bool IsProjectReady => CoreFacadeState.ProjectInit;
        protected bool IsSceneReady => CoreFacadeState.SceneInit;

        protected IEnumerator _deferredSpawnRoutine;
        protected IEnumerator DeferredspawnRoutine => _deferredSpawnRoutine;

        // 위버가 코드를 안전하게 찔러넣을 수 있는 공간
        // 컴파일 시 FishNet Weaver가 여기에 NetworkInitialize___Early() 등을 몰래 주입할 수 있음
        public virtual void Awake() { }

        #region Update
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
        #endregion

        #region Spawn

        /// <summary>
        /// 네트워크 Spawn 이벤트의 진입점
        /// Scene 초기화가 완료된 후 실제 Spawn 로직을 실행
        /// </summary>
        public void OnSpawn()
        {
            if (_deferredSpawnRoutine != null)
                return;

            _deferredSpawnRoutine = DeferredSpawnRoutine();
            StartCoroutine(_deferredSpawnRoutine);
        }

        private IEnumerator DeferredSpawnRoutine()
        {
            // Scene 시스템이 준비될 때까지 대기
            while (!IsSceneReady)
                yield return null;

            // 대기 도중 Despawn되어 Pool로 반환되었거나
            // NetworkObject가 더 이상 Spawn 상태가 아니라면 종료
            if (this == null ||
                !gameObject.activeInHierarchy ||
                !NetworkObject.IsSpawned)
            {
                _deferredSpawnRoutine = null;
                yield break;
            }

            // Root 객체라면 Object 설정에 따라 Scene 이동
            if (transform.parent == null)
            {
                var scene = NetworkObject.IsGlobal
                    ? CoreFacade.GetGlobalScene()
                    : CoreFacade.GetCurrentScene();

                gameObject.MoveScene(scene);
            }

            // Scene 인프라와 객체 상태가 보장된 시점에서 실제 Spawn 로직 실행
            OnSafeSpawn();

            _deferredSpawnRoutine = null;
        }

        /// <summary>
        /// Scene 인프라가 준비되고 객체가 유효한 상태에서 실행되는 실제 Spawn 로직
        /// 자식 클래스는 이 메서드를 재정의하여 Spawn 시 필요한 초기화를 수행
        /// </summary>
        protected virtual void OnSafeSpawn() { }

        public void OnDespawn()
        {
            // Spawn 대기 중이었다면 Coroutine을 종료
            if (_deferredSpawnRoutine != null)
            {
                StopCoroutine(_deferredSpawnRoutine);
                _deferredSpawnRoutine = null;
            }

            OnSafeDespawn();
        }

        /// <summary>
        /// Despawn 시 실행되는 실제 정리 로직
        /// </summary>
        protected virtual void OnSafeDespawn() { }


        #endregion
    }
}