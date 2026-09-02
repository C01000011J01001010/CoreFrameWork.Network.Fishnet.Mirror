using CoreEngine.Manager.Pool;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Server;
using System;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Manager.Pool
{
    /// <summary>
    /// 순수 C# 풀러(PoolHandler)를 상속받아 네트워크 통제권만 덧씌운 부품
    /// </summary>
    public class NetObjectPoolHandler<TPoolType> : BasePoolHandler<TPoolType> where TPoolType : System.Enum
    {
        // Pool에서 Spawn -> OnSpawn -> ServerManager.Spawn
        public override GameObject Spawn(Vector3 position)
        {
            // 서버 권한 없이 순수 클라이언트로 접속한 경우 스폰 로직을 즉시 탈출
            if (!InstanceFinder.IsServerStarted) return null;

            GameObject obj = base.Spawn(position); // -> OnSpawn() 호출됨
            InstanceFinder.ServerManager.Spawn(obj);
            return obj;
        }

        // ServerManager.Despawn -> Pool에서 Release -> OnDespawn
        public override void Release(GameObject obj)
        {
            // 서버 권한 없이 순수 클라이언트로 접속한 경우 스폰 로직을 즉시 탈출
            if (!InstanceFinder.IsServerStarted) return;
            if (obj == null) return;

            // FishNet 서버에 네트워크 단절 요청
            InstanceFinder.ServerManager.Despawn(obj); 

            base.Release(obj); // -> OnDespawn() 호출됨
        }
    }
}