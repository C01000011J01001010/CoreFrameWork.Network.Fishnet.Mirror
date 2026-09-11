using FishNet.Object;
using System;
using UnityEngine;
using CoreEngine.Pool;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoreEngine.Network.FishNetExtension.Pool
{
    public abstract class BaseNetObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, NetObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {
        [Header("전역 스폰 라우터 연결")]
        [SerializeField] private NetSpawnRouterSO _spawnRouterSO; // 인스펙터 할당 또는 OnValidate 자동 추적

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_spawnRouterSO == null) return;

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                var netObj = setup.prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // 💡 람다식 파라미터(위치, 회전, 부모)를 누락 없이 온전히 전달
                    _spawnRouterSO.RegisterHandler(netObj.PrefabId,
                        (pos, rot, parent) => base.Spawn(setup.poolType, pos, rot, parent),
                        (pObj) => base.Release(setup.poolType, pObj));
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_spawnRouterSO == null) return;

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                var netObj = setup.prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    _spawnRouterSO.UnregisterHandler(netObj.PrefabId);
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // 라우터 SO 자동 찾기 로직
            if (_spawnRouterSO == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:NetSpawnRouterSO");
                if (guids.Length > 0)
                {
                    _spawnRouterSO = AssetDatabase.LoadAssetAtPath<NetSpawnRouterSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            foreach (var setup in poolSetups)
            {
                if (setup.prefab == null) continue;

                if (!setup.prefab.TryGetComponent(out IPoolable _) || !setup.prefab.TryGetComponent(out NetworkObject netObj))
                {
                    Debug.LogError($"[{setup.prefab.name}] 조건 불만족 (IPoolable, NetworkObject)");
                    setup.prefab = null;
                    continue;
                }

                // 💡 불필요해진 hijackedPrefabs 추가 로직 삭제 완료
            }
        }
#endif
    }
}