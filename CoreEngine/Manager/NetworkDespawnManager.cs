using CoreEngine;
using CoreEngine.Helpers;
using CoreEngine.Manager;
using CoreEngine.Network.FishNetExtension;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;

public class NetworkDespawnManager : BaseNetworkManager, IPriority
{
    public int Priority => (int)ManagerPriority.Infrastructure;

    protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

    [ServerRpc(RequireOwnership = false)]
    public void RequsetDespawnServerRpc(NetworkObject netObject, DespawnType? despawnType = null, NetworkConnection caller = null)
    {
        if (netObject == null || caller == null) return;
        if (DespawnCondition(netObject, caller))
        {
            ServerManager.Despawn(netObject, despawnType);
        }
    }

    /// <summary>
    /// 프로젝트 따라 조건 재정의 가능
    /// </summary>
    /// <returns></returns>
    public virtual bool DespawnCondition(NetworkObject netObject, NetworkConnection caller)
    {
        // 객체의 소유자이거나 Host인 경우 Despawn 허용
        return netObject.Owner == caller || caller.IsHost;
    }
}
