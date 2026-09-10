
using CoreEngine.EventBus;
using CoreEngine.Hub;
using CoreEngine.Pool;
using System.Collections;

namespace CoreEngine.Network.FishNetExtension
{
    // 멀티플레이 객체용 3계층 Leaf 기본 클래스
    public abstract class BaseNetworkActor : BaseNetworkLeaf, IActor
    {
        private IPoolable _cachedPoolable;
        public override void Awake()
        {
            base.Awake();
            _cachedPoolable = GetComponent<IPoolable>();
        }
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            // 이 시점에는 IsServer, OwnerId 등 네트워크 정보가 완벽히 세팅되어 있습니다.
            var evt = new ActorRegistrationEvent(this, true, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);

            // ServerManager에 의해 온전히 Spawn이 완료될 때 OnSpawn 처리
            if (_cachedPoolable != null)
                _cachedPoolable.OnSpawn();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Hub에 내가 안 쓰임을 알림
            var evt = new ActorRegistrationEvent(this, false, myScope);
            EventBus<ActorRegistrationEvent>.Publish(evt);

            // Releaser(PoolHandler)는 순수C#객체이니 ?.Release로 null 판별
            if (_cachedPoolable != null)
                _cachedPoolable.Releaser?.Release(_cachedPoolable);
        }

        protected IEnumerator _deferredSpawnRoutine;
        protected IEnumerator DeferredspawnRoutine => _deferredSpawnRoutine;

        /// <summary>
        /// 자식 클래스에서 OnSpawn() 대신 호출할 안전한 스폰 진입점
        /// </summary>
        public void SafeSpawn()
        {
            if (_deferredSpawnRoutine != null) return;
            _deferredSpawnRoutine = DeferredSpawnRoutine();
            StartCoroutine(_deferredSpawnRoutine);
        }

        private IEnumerator DeferredSpawnRoutine()
        {
            // 씬 시스템(ManagerHub 등)이 준비될 때까지 대기
            while (!IsSceneReady) yield return null;

            // 대기 도중 풀로 반환(비활성)되거나 파괴되었다면 즉시 탈출
            if (this == null || !gameObject.activeInHierarchy)
            {
                _deferredSpawnRoutine = null;
                yield break;
            }

            // 안전한 타이밍에 자식 클래스의 실제 로직 실행
            OnSafeSpawn();
            _deferredSpawnRoutine = null;
        }

        /// <summary>
        /// 씬 인프라가 보장되고, 객체가 활성화된 상태임이 확실할 때 실행되는 스폰 로직
        /// 자식 클래스는 이 메서드를 오버라이드하여 부품 조립 및 상태 시동을 진행
        /// </summary>
        protected virtual void OnSafeSpawn()
        {
        }
    }
}
