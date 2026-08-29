using FishNet;
using FishNet.Managing.Scened;
using CoreEngine.EventBus;
using UnityEngine;
using CoreEngine.Director;

namespace CoreEngine.Network.FishNetExtension
{
    public class NetworkSceneFlowDirector : SceneFlowDirector
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            if (InstanceFinder.SceneManager != null)
            {
                InstanceFinder.SceneManager.OnLoadStart += OnNetworkSceneLoadStart;
                InstanceFinder.SceneManager.OnLoadPercentChange += OnNetworkSceneLoadProgress;
                InstanceFinder.SceneManager.OnLoadEnd += OnNetworkSceneLoadEnd;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (InstanceFinder.SceneManager != null)
            {
                InstanceFinder.SceneManager.OnLoadStart -= OnNetworkSceneLoadStart;
                InstanceFinder.SceneManager.OnLoadPercentChange -= OnNetworkSceneLoadProgress;
                InstanceFinder.SceneManager.OnLoadEnd -= OnNetworkSceneLoadEnd;
            }
        }

        // 방장의 네트워크 씬 로딩 요청을 가로채서 FishNet API로 전환
        protected override void OnSceneLoadRequest(SceneLoadRequestEvent evt)
        {
            if (InstanceFinder.IsServerStarted)
            {
                SceneLoadData sld = new SceneLoadData(evt.TargetSceneName);

                // 기존 네트워크 씬을 모두 교체하고 새로운 씬을 엽니다.
                // GlobalScene은 FishNet 외부에서 열렸으므로 파괴되지 않고 유지됩니다.
                sld.ReplaceScenes = ReplaceOption.All;

                InstanceFinder.SceneManager.LoadGlobalScenes(sld);
            }
            else
            {
                // 오프라인 상태라면 부모 클래스의 유니티 SceneManager 방식을 사용합니다.
                base.OnSceneLoadRequest(evt);
            }
        }

        private void OnNetworkSceneLoadStart(SceneLoadStartEventArgs args)
        {
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Start, "로딩 시작", 0.0f));
        }

        private void OnNetworkSceneLoadProgress(SceneLoadPercentEventArgs args)
        {
            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "자원 불러오는 중...", args.Percent));
        }

        

        private void OnNetworkSceneLoadEnd(SceneLoadEndEventArgs args)
        {
            StartCoroutine(InitializeSceneSystemRoutine());
        }
    }
}