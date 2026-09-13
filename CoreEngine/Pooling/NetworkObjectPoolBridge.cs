using FishNet.Utility.Performance;
using FishNet.Object;
using UnityEngine;
using CoreEngine.Interface;

namespace CoreEngine.Network.FishNetExtension.Pool
{
    public class NetworkObjectPoolBridge : ObjectPool
    {
        // PoolManager가 Spawn처리를 등록하는 대리자
        private readonly NetSpawnRouter _spawnRouter = new();

        InterfacePublisher<INetworkSpawnDelegate> _register;

        private void Awake()
        {
            _register = new(_spawnRouter);
            _register.Bind();
        }

        private void OnDestroy()
        {
            _register.Unbind();
        }

        // FishNet이 Spawn 처리를 위해 로컬 NetworkObject를 요청할 때
        // 어떤 객체를 사용할지 처리
        public override NetworkObject RetrieveObject(int prefabId, ushort collectionId, ObjectPoolRetrieveOption options, Transform parent = null, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, bool asServer = true)
        {
            // FishNet 엔진에서 원본 프리팹 데이터 추출
            NetworkObject prefab = base.NetworkManager.SpawnablePrefabs.GetObject(true, prefabId);

            Vector3 safePosition = position ?? Vector3.zero;
            Quaternion safeRotation = rotation ?? Quaternion.identity;

            return _spawnRouter.RetrieveObject(prefab, safePosition, safeRotation);
        }

        // FishNet이 Despawn된 NetworkObject를 반환할 때
        // 어떤 방식으로 정리할지 처리
        public override void StoreObject(NetworkObject instantiated, bool asServer)
        {
            _spawnRouter.StoreObject(instantiated);
        }
    }
}