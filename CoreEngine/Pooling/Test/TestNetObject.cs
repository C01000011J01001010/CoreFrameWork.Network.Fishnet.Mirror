using CoreEngine.Helpers;
using CoreEngine.Pool;
using CoreEngine.Pool.Test;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Pool.Test
{
    public class TestNetObject : CoreNetworkBehaviour, IPoolable
    {
        public IPoolReleaser Releaser { get; set; }

        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

        int id;

        private void Start()
        {
            id = gameObject.GetInstanceID();
        }

        protected override void OnSafeSpawn()
        {
            LogHelper.Log($"({gameObject.name}.{id}) 등장", LogColor.Green);
            TestPoolTracker.SpawnedObjects.Add(this);
        }

        protected override void OnSafeDespawn()
        {
            if(TestPoolTracker.SpawnedObjects.Remove(this))
            {
                LogHelper.Log($"({gameObject.name}.{id}) 퇴장", LogColor.Blue);
            }
        }
    }

}
