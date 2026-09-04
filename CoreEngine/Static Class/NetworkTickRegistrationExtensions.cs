using FishNet.Object;
using CoreEngine.Extensions;

namespace CoreEngine.Network.FishNetExtension.Extensions
{
    public static class NetworkTickRegistrationExtensions
    {
        public static void TryRegisterNetworkTick(this NetworkBehaviour netBehaviour, ref bool isRegistered, NetworkTickTarget target)
        {
            if (isRegistered) return;
            if (!netBehaviour.EvaluateNetworkCondition(target)) return;

            isRegistered = true;
            netBehaviour.RegisterTick(); // 실제 작업은 위임
        }

        public static void TryUnregisterNetworkTick(this NetworkBehaviour netBehaviour, ref bool isRegistered)
        {
            if (!isRegistered) return;

            isRegistered = false;
            netBehaviour.UnregisterTick(); // 실제 작업은 위임
        }

        private static bool EvaluateNetworkCondition(this NetworkBehaviour netBehaviour, NetworkTickTarget target)
        {
            return target switch
            {
                NetworkTickTarget.None => false,
                NetworkTickTarget.ServerOnly => netBehaviour.IsServerInitialized,
                NetworkTickTarget.ClientOnly => netBehaviour.IsClientInitialized,
                NetworkTickTarget.OwnerOnly => netBehaviour.IsOwner,
                NetworkTickTarget.ServerAndClient => netBehaviour.IsServerInitialized || netBehaviour.IsClientInitialized,
                _ => false
            };
        }
    }
}