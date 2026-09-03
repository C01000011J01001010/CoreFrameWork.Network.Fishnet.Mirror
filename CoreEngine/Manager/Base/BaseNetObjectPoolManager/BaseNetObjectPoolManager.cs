using CoreEngine.Manager;
using CoreEngine.Manager.Pool;
using CoreEngine.Network.FishNetExtension.Manager.Pool;
using FishNet;
using FishNet.Object;
using System;
using UnityEngine;
using static UnityEditor.MaterialProperty;

namespace CoreEngine.Network.FishNetExtension.Manager
{
    /// <summary>
    /// 로컬 풀링 시스템을 상속받아 네트워크 스폰/디스폰 통제권을 결합한 매니저
    /// </summary>
    public abstract class BaseNetObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, NetObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {
        protected override void OnValidate()
        {
            base.OnValidate();
            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                bool hasPoolable = setup.prefab.TryGetComponent(out IPoolable _);
                bool hasNetObject = setup.prefab.TryGetComponent(out NetworkObject _);

                // 둘 중 하나라도 없으면 네트워크 풀링에 부적합하므로 차단
                if (!hasPoolable || !hasNetObject)
                {
                    Debug.LogError($"[{setup.prefab.name}]은(는) {nameof(BaseNetObjectPoolManager<TPoolType>)}의 조건을 만족하지 않습니다.\n({nameof(IPoolable)}과 {nameof(NetworkObject)} 모두 필요)");
                    setup.prefab = null;
                }
            }

        }
    }
}