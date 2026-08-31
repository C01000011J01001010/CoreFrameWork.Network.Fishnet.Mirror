using CoreEngine.EventBus;
using CoreEngine.Ui;
using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CoreEngine.Network.FishNetExtension
{
    public class NetworkConnectionUI : MonoBehaviour
    {
        [Header("--- Common Settings ---")]
        [Tooltip("진행 상황을 실시간으로 안내하는 텍스트")]
        [SerializeField] private TextMeshProUGUI statusDisplayText;
        [Tooltip("호스트, 서버, 클라이언트 공통 포트")]
        [SerializeField] private TMP_InputField portInputField;

        [Header("--- Host / Server (방 만들기) ---")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button serverButton;

        [Header("--- Client (방 참가하기) ---")]
        [Tooltip("접속할 힐링 농장의 IP 주소")]
        [SerializeField] private TMP_InputField clientIpInputField;
        [SerializeField] private Button clientButton;

        private CancellationTokenSource _cts;
        private ConnectionMode _lastRequestedMode;
        private Task<string> _ipFetchTask;

        private void Awake()
        {
            portInputField.text = "7770";
            clientIpInputField.text = "localhost";
            if (statusDisplayText != null) statusDisplayText.text = "대기 중...";

            hostButton.onClick.AddListener(() => RequestConnection(ConnectionMode.Host));
            serverButton.onClick.AddListener(() => RequestConnection(ConnectionMode.Server));
            clientButton.onClick.AddListener(() => RequestConnection(ConnectionMode.Client));

            EventBus<NetworkConnectionLostEvent>.Subscribe(OnNetworkConnectionLost);
        }

        private void OnDestroy()
        {
            EventBus<NetworkConnectionLostEvent>.Unsubscribe(OnNetworkConnectionLost);
        }

        private void OnNetworkConnectionLost(NetworkConnectionLostEvent evt)
        {
            gameObject.SetActive(true);
            EnableButtons();
        }

        private void OnEnable()
        {
            EventBus<NetworkConnectionSuccessEvent>.Subscribe(OnConnectionSuccess);
            EventBus<NetworkConnectionFailEvent>.Subscribe(OnConnectionFail);
        }

        private void OnDisable()
        {
            EventBus<NetworkConnectionSuccessEvent>.Unsubscribe(OnConnectionSuccess);
            EventBus<NetworkConnectionFailEvent>.Unsubscribe(OnConnectionFail);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void RequestConnection(ConnectionMode mode)
        {
            DisableButtons();
            _lastRequestedMode = mode;
            _cts = new CancellationTokenSource();

            string ip = string.IsNullOrEmpty(clientIpInputField.text) ? "localhost" : clientIpInputField.text;
            ushort port = ushort.TryParse(portInputField.text, out ushort p) ? p : (ushort)7770;

            if (mode == ConnectionMode.Client)
            {
                if (statusDisplayText != null) statusDisplayText.text = "접속 중...";
            }
            else
            {
                if (statusDisplayText != null) statusDisplayText.text = "방 개설 중...";
                // 팝업에서 사용할 IP를 미리 비동기로 요청해둡니다.
                _ipFetchTask = FetchPublicIpTask(_cts.Token);
            }

            EventBus<ConnectRequestEvent>.Publish(new ConnectRequestEvent
            {
                Mode = mode,
                IpAddress = ip,
                Port = port
            });
        }

        private void OnConnectionSuccess(NetworkConnectionSuccessEvent evt)
        {
            if (_lastRequestedMode == ConnectionMode.Client)
            {
                EventBus<SpawnPopUpEvent>.Publish(new SpawnPopUpEvent(
                "[서버 접속 성공]",
                PopUpType.Confirm
                ));

                // 팝업창 띄운 후 종료
                gameObject.SetActive(false);
            }
            else
            {
                // Host, Server 모드일 경우 IP 로드를 마저 기다린 후 팝업 호출
                ProcessHostSuccessAsync();
            }
            
        }

        private async void ProcessHostSuccessAsync()
        {
            if (statusDisplayText != null) statusDisplayText.text = "IP 주소 확인 중...";

            string publicIp = "IP 로드 실패";
            if (_ipFetchTask != null)
            {
                publicIp = await _ipFetchTask;
            }

            // Task 대기 중 오브젝트가 파괴되거나 꺼졌다면 안전하게 중단
            if (_cts == null || _cts.IsCancellationRequested) return;

            // 팝업 발송 (Action 콜백으로 클립보드 복사 캡슐화)

            Action copyCompletePopUpRequest = ()=>
                EventBus<SpawnPopUpEvent>.Publish(new SpawnPopUpEvent(
                $"[클립보드 복사 완료]\n동료에게 주소를 알려주세요:\n{publicIp}",
                PopUpType.Confirm
            ));

            EventBus<SpawnPopUpEvent>.Publish(new SpawnPopUpEvent(
                "[서버 개설 성공]\n확인 버튼을 누르면\n접속을 위한 ip를 복사합니다.",
                PopUpType.Confirm,
                () =>
                {
                    GUIUtility.systemCopyBuffer = publicIp;
                    copyCompletePopUpRequest?.Invoke();
                }
            ));

            // 팝업창 띄운 후 종료
            gameObject.SetActive(false);
        }

        private void OnConnectionFail(NetworkConnectionFailEvent evt)
        {
            string displayMessage = $"[접속 실패]\n{evt.ErrorMessage}";
            if (statusDisplayText != null) statusDisplayText.text = "접속 실패";

            EventBus<SpawnPopUpEvent>.Publish(new SpawnPopUpEvent(
                displayMessage,
                PopUpType.Confirm,
                () => EnableButtons()
            ));
        }

        private void DisableButtons()
        {
            hostButton.interactable = false;
            serverButton.interactable = false;
            clientButton.interactable = false;
        }

        private void EnableButtons()
        {
            hostButton.interactable = true;
            serverButton.interactable = true;
            clientButton.interactable = true;
            if (statusDisplayText != null) statusDisplayText.text = "대기 중...";
        }

        private async Task<string> FetchPublicIpTask(CancellationToken token)
        {
            using (UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org"))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (token.IsCancellationRequested) return "취소됨";
                    await Task.Yield();
                }

                return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : "IP 로드 실패";
            }
        }
    }
}