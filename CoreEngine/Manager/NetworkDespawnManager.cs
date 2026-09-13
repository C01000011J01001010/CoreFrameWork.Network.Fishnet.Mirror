using CoreEngine;
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
        if (caller == null) return;
        if (netObject.Owner == caller || caller.IsHost)
        {
            ServerManager.Despawn(netObject, despawnType);
        }
    }
}
