using FishNet.Utility.Performance;
using FishNet.Object;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Pool
{
    public class FishNetPoolBridge : FishNet.Utility.Performance.ObjectPool
    {
        [Header("연결할 전역 스폰 라우터")]
        [SerializeField] private NetSpawnRouterSO _spawnRouterSO;

        public override NetworkObject RetrieveObject(int prefabId, ushort collectionId, ObjectPoolRetrieveOption options, Transform parent = null, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool asServer = true)
        {
            // 1. FishNet 엔진에서 원본 프리팹 데이터 추출
            NetworkObject prefab = base.NetworkManager.SpawnablePrefabs.GetObject(true, prefabId);

            // 💡 2. Nullable 매개변수 안전 추출 (null일 경우 기본값 할당)
            Vector3 safePosition = position ?? Vector3.zero;
            Quaternion safeRotation = rotation ?? Quaternion.identity;

            // 3. 유저님의 SO 라우터로 토스
            return _spawnRouterSO.RetrieveObject(prefab, safePosition, safeRotation);
        }

        public override void StoreObject(NetworkObject instantiated, bool asServer)
        {
            _spawnRouterSO.StoreObject(instantiated);
        }
    }
}