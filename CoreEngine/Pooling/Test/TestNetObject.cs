using CoreEngine.Helpers;
using CoreEngine.Pool;
using CoreEngine.Pool.Test;
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

        public void OnDespawn()
        {
            LogHelper.Log($"({gameObject.name}.{id}) 퇴장", LogColor.Blue);
        }

        public void OnSpawn()
        {
            LogHelper.Log($"({gameObject.name}.{id}) 등장", LogColor.Green);
            TestPoolTracker.SpawnedObjects.Push(this);
        }
    }

}
