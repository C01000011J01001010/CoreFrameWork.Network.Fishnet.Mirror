using CoreEngine.Facades;
using CoreEngine.Pool;
using FishNet.Object;
namespace CoreEngine.Network.FishNetExtension.Pool
{
    /// <summary>
    /// 순수 C# 풀러(PoolHandler)를 상속받아 네트워크 통제권만 덧씌운 부품
    /// </summary>
    public class NetObjectPoolHandler<TPoolType> : ObjectPoolHandler<TPoolType> where TPoolType : System.Enum
    {
        NetworkDespawnManager NetDespawnManager;

        public override void Release(IPoolable pObj)
        {
            // 널 체크 및 ! 연산자를 통한 정확한 네트워크 객체 판별
            if (pObj == null || !pObj.gameObject.TryGetComponent(out NetworkObject netObject)) return;

            if (netObject.IsSpawned)
            {
                if(NetDespawnManager == null) NetDespawnManager = CoreFacade.GetManager<NetworkDespawnManager>();
                NetDespawnManager?.RequsetDespawnServerRpc(netObject, DespawnType.Pool);
            }
            else
            {
                // 서버의 Despawn 패킷을 받은 깡통상태
                // 방장과 참가자 모두 이 분기를 타고 로컬 풀에  회수됨
                base.Release(pObj);
            }
        }
    }
}