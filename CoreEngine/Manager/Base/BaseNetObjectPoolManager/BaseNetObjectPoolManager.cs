using FishNet.Object;
using System;
using UnityEngine;
using CoreEngine.Pool;
using CoreEngine.Interface;
using CoreEngine.Helpers;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.Network.FishNetExtension.Pool
{
    public abstract class BaseNetObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, NetObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {
        protected readonly InterfaceReceiver<INetworkSpawnDelegate> _poolRegisterReceiver = new();

        protected override void Awake()
        {
            base.Awake();
            _poolRegisterReceiver.Bind();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _poolRegisterReceiver.Unbind();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (!_poolRegisterReceiver.TryGet(out INetworkSpawnDelegate SpawnDelegater)) return;

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                var netObj = setup.prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // 람다식 파라미터(위치, 회전, 부모)를 누락 없이 온전히 전달
                    SpawnDelegater.Register(netObj.PrefabId,
                        (pos, rot, parent) => base.Spawn(setup.poolType, pos, rot, parent),
                        (pObj) => base.Release(setup.poolType, pObj));
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!_poolRegisterReceiver.TryGet(out INetworkSpawnDelegate SpawnDelegater)) return;

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                var netObj = setup.prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    SpawnDelegater.Unregister(netObj.PrefabId);
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                if (!setup.prefab.TryGetComponent(out NetworkObject netObj))
                {
                    Debug.LogError($"[{setup.prefab.name}]은 NetworkObject 컴포넌트가 없음");
                    setup.prefab = null;
                    continue;
                }
            }
        }
#endif
    }
}