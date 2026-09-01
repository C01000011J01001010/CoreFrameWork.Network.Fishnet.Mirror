using CoreEngine;
using CoreEngine.EventBus;
using CoreEngine.Hub;
using System.Collections;

namespace CoreEngine.Network.FishNetExtension
{
    /// <summary>
    /// 멀티플레이 환경에서 동작하는 단일 시스템 모듈(IModule)을 위한 기본 클래스
    /// </summary>
    public abstract class BaseNetworkModule : BaseNetworkLeaf, IModule
    {
        private bool isActive;
        public bool IsActive => isActive;

        public virtual void Exit() { }

        public virtual IEnumerator Initialize() { yield return null; }

        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
            isActive = active;
        }

        // 유니티 생명주기(Awake) 대신 FishNet 전용 콜백 사용
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            // 네트워크 세팅(IsServer, OwnerId 등)이 완벽히 끝난 시점에 Hub에 등록
            var evt = new ModuleRegistrationEvent(this, true, myScope);
            EventBus<ModuleRegistrationEvent>.Publish(evt);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // 네트워크 연결 해제 시 Hub에서 안전하게 제거
            var evt = new ModuleRegistrationEvent(this, false, myScope);
            EventBus<ModuleRegistrationEvent>.Publish(evt);
        }
    }
}