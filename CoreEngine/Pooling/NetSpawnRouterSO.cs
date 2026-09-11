using CoreEngine.Pool;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Pool
{
    [CreateAssetMenu(fileName = "NetSpawnRouterSO", menuName = "CoreEngine/Network/NetSpawnRouterSO")]
    public class NetSpawnRouterSO : ScriptableObject
    {
        // 런타임 델리게이트 딕셔너리
        private Dictionary<int, Func<Vector3, Quaternion, Transform, IPoolable>> _spawnDelegates = new();
        private Dictionary<int, Action<IPoolable>> _despawnDelegates = new();

        public void RegisterHandler(int prefabId, Func<Vector3, Quaternion, Transform, IPoolable> spawnFunc, Action<IPoolable> despawnAction)
        {
            _spawnDelegates[prefabId] = spawnFunc;
            _despawnDelegates[prefabId] = despawnAction;
        }

        public void UnregisterHandler(int prefabId)
        {
            _spawnDelegates.Remove(prefabId);
            _despawnDelegates.Remove(prefabId);
        }

        // 💡 FishNet 브릿지가 객체를 요구할 때 호출
        public NetworkObject RetrieveObject(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab != null && _spawnDelegates.TryGetValue(prefab.PrefabId, out var spawnFunc))
            {
                IPoolable pObj = spawnFunc(position, rotation, null);
                return pObj.gameObject.GetComponent<NetworkObject>();
            }

            // 등록된 씬 풀이 없다면 기본 Instantiate 수행
            return Instantiate(prefab.gameObject, position, rotation).GetComponent<NetworkObject>();
        }

        // 💡 FishNet 브릿지가 객체를 버릴 때 호출
        public void StoreObject(NetworkObject netObject)
        {
            if (netObject.TryGetComponent(out IPoolable pObj) && _despawnDelegates.TryGetValue(netObject.PrefabId, out var despawnAction))
            {
                despawnAction(pObj);
            }
            else
            {
                Destroy(netObject.gameObject);
            }
        }
    }
}