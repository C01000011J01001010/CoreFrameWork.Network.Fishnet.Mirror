using CoreEngine.Manager.Pool;
using FishNet;
using FishNet.Object;
using UnityEngine;
using System.Threading.Tasks;

namespace CoreEngine.Network.FishNetExtension.Manager.Pool
{
    /// <summary>
    /// 순수 C# 풀러(PoolHandler)를 상속받아 네트워크 통제권만 덧씌운 부품
    /// </summary>
    public class NetObjectPoolHandler<TPoolType> : BasePoolHandler<TPoolType> where TPoolType : System.Enum
    {
        // Pool에서 Spawn -> ServerManager.Spawn -> OnSpawn
        public override IPoolable Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // 서버 권한 없이 순수 클라이언트로 접속한 경우 스폰 로직을 즉시 탈출
            if (!InstanceFinder.IsServerStarted) return null;

            IPoolable pObj = base.Spawn(position, rotation);
            InstanceFinder.ServerManager.Spawn(pObj.gameObject);
            // OnSpawn은 CoreNetworkBehaviour의 OnStartNetwork에서 처리
            return pObj;
        }

        // ServerManager.Despawn -> Pool에서 Release -> OnDespawn
        public override void Release(IPoolable pObj)
        {
            // 널 체크 및 ! 연산자를 통한 정확한 네트워크 객체 판별
            if (pObj == null || !pObj.gameObject.TryGetComponent(out NetworkObject netObject)) return;

            if (netObject.IsSpawned)
            {
                // 통신망에 살아있는 객체는 오직 서버(Host)만이 삭제 권한을 가짐
                if (InstanceFinder.IsServerStarted)
                {
                    InstanceFinder.ServerManager.Despawn(netObject, DespawnType.Pool);
                }
                // 클라이언트는 서버 권위가 없으니 아무것도 안함
            }
            else
            {
                // 서버의 Despawn 패킷을 받고 객체의 OnStopNetwork를 거쳐 들어온 깡통 상태
                // 방장과 참가자 모두 정확히 이 분기를 타고 로컬 풀에 즉시 회수됨
                base.Release(pObj);
            }
        }
    }
}