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
        private bool _isInit;
        private bool _isActive;

        bool IModule.IsInit => _isInit;
        bool IModule.IsActive => _isActive;

        bool IModule.GetIsInit() => _isInit;
        bool IModule.GetIsActive() => _isActive;

        IEnumerator IModule.Initialize()
        {
            if (_isInit) yield break;
            yield return OnInitialize();
            _isInit = true;
        }
        protected virtual IEnumerator OnInitialize() { yield break; }

        void IModule.Exit()
        {
            OnExit();
            _isInit = false;
        }
        public virtual void OnExit() { }

        void IModule.SetActive(bool active)
        {
            // 시스템적으로 활성화 여부와 논리적 활성화 여부를 분리
            if (active == GetSystemActive() &&
                active == _isActive)
                return;

            ActiveMethod(active);
            _isActive = active;

            OnSetActive(active);
        }
        protected virtual bool GetSystemActive() { return gameObject.activeInHierarchy; }
        protected virtual void ActiveMethod(bool active)
        {
            if (active && gameObject.activeSelf && !gameObject.activeInHierarchy)
            {
                UnityEngine.Transform parent = transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        parent.gameObject.SetActive(true);
                        break;
                    }

                    parent = parent.parent;
                }
            }
            gameObject.SetActive(active);
        }
        protected virtual void OnSetActive(bool active) { }



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