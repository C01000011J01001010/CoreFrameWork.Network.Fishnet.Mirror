using CoreEngine.Extentions;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension
{
    /// <summary>
    /// 네트워크 객체를 3계층(Leaf)으로 편입시키고 스코프를 관리하는 클래스
    /// </summary>
    public abstract class BaseNetworkLeaf : CoreNetworkBehaviour
    {
        // 어느 Context 산하로 들어갈지 결정
        [SerializeField] protected ContextScope myScope;

        public void SetScope(ContextScope scope)
        {
            myScope = scope;
            OnSetScope(scope);
        }

        protected virtual void OnSetScope(ContextScope scope)
        {

        }

#if UNITY_EDITOR
        // 유니티 에디터에서 값이 변경되거나, 씬에 배치될 때 자동 호출되는 함수
        protected override void OnValidate()
        {
            base.OnValidate();
            this.AutoSetupScope(ref myScope);
        }
#endif
    }
}