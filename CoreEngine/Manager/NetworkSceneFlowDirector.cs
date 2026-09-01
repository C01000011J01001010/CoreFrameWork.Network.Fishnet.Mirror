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

                // 이 시점에서 Scene의 Awake가 실행 후 OnLoadEnd 실행
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
                // 네트워크 상태일 때만 자식 클래스에서 중복 실행을 잠금
                if (_isRoutine) return;
                _isRoutine = true;

                // GlobalScene 보호: FishNet이 네트워크로 로드된 씬만 교체하도록 설정
                SceneLoadData sld = new SceneLoadData(evt.TargetSceneName);
                sld.ReplaceScenes = ReplaceOption.OnlineOnly;
                InstanceFinder.SceneManager.LoadGlobalScenes(sld);
            }
            else
            {
                // 오프라인 상태라면 부모 클래스의 유니티 SceneManager 방식을 사용
                // 오프라인 상태라면 방어 로직( _isRoutine 체크 )까지 부모에게 완전히 위임
                base.OnSceneLoadRequest(evt);
            }
        }

        private void OnNetworkSceneLoadStart(SceneLoadStartEventArgs args)
        {
            // 호스트 모드일 때 서버 측에서 불린 콜백은 무시 (데디케이티드 서버가 아닐 경우)
            if (!IsValidCallback(args.QueueData.AsServer)) return;

            // 모든 클라이언트 동기화: 방장과 참가자 모두 여기서 이전 오프라인 씬(TitleScene 등)을 정리
            if (currentScene.IsValid() && currentScene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);

                // 이전 씬 찌꺼기가 새 씬 로딩에 개입하는 것을 막기 위해 백지화
                currentScene = default;

                // 새로운 currentScene은 OnLoadEnd 직전 SceneContext가 Awake에서 설정함
            }

            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Start, "로딩 시작", 0.0f));
        }

        private void OnNetworkSceneLoadProgress(SceneLoadPercentEventArgs args)
        {
            if (args.QueueData.AsServer && !InstanceFinder.IsServerOnlyStarted) return;

            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Progress, "자원 불러오는 중...", args.Percent));
        }

        

        private void OnNetworkSceneLoadEnd(SceneLoadEndEventArgs args)
        {
            if (args.QueueData.AsServer && !InstanceFinder.IsServerOnlyStarted) return;

            EventBus<SystemLoadingEvent>.Publish(new SystemLoadingEvent(SystemLoadingEvent.State.Complete, "로딩 완료!", 1.0f));
            StartCoroutine(InitializeSceneSystemRoutine());
        }

        // 현재 접속 환경(Host/Client)에 맞는 '첫 번째' 콜백만 통과시키는 헬퍼 함수
        private bool IsValidCallback(bool asServer)
        {
            if (InstanceFinder.IsServerStarted)
            {
                // 방장(Host)이거나 데디케이티드 서버면, 가장 먼저 들어오는 서버 콜백(true)일 때 통과
                return asServer;
            }
            else
            {
                // 순수 참가자(Client)면, 클라이언트 콜백(false)일 때 통과
                return !asServer;
            }
        }
    }
}