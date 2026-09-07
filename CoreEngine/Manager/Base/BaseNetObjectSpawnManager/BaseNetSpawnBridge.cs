using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Spawn
{
    /// <summary>
    /// FishNet IL Weaver의 제네릭 충돌을 방지하기 위한 비제네릭 RPC 브릿지
    /// </summary>
    public abstract class BaseNetSpawnBridge : BaseNetworkManager
    {
        [ServerRpc(RequireOwnership = false)]
        protected void RequestSpawnServerRpc(int poolTypeInt, Vector3 position, Quaternion rotation, bool isOwner, NetworkObject parentNetObj, NetworkConnection caller = null)
        {
            OnServerSpawnRequested(poolTypeInt, position, rotation, isOwner, parentNetObj, caller);
        }

        /// <summary>
        /// 서버에서 RPC 수신 시 호출되는 가상 메서드 (자식 제네릭 클래스에서 로직 오버라이드)
        /// </summary>
        protected abstract void OnServerSpawnRequested(int poolTypeInt, Vector3 position, Quaternion rotation, bool isOwner, NetworkObject parentNetObj, NetworkConnection caller);
    }
}