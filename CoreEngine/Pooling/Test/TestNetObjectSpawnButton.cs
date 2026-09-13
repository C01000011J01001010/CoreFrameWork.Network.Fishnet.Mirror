using UnityEngine;
using CoreEngine.Pool.Test;
using CoreEngine.EventBus;
using CoreEngine.Network.FishNetExtension.Spawn;

namespace CoreEngine.Network.FishNetExtension.Pool.Test
{
    public class TestNetObjectSpawnButton : BaseTestSpawnButton<TestPoolType>
    {
        [SerializeField]
        private bool _isOwner = false;

        [SerializeField]
        private bool _isGlobal = false;


        protected override void OnClickSpawn()
        {
            Vector3 randomPos = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            Vector3 randomRot = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            RequestSpawnData<TestPoolType> _spawnData = new(targetPoolType, randomPos, randomRot);

            SpawnRequestEvent<TestPoolType> evt = new(_spawnData, _isOwner, _isGlobal);
            EventBus<SpawnRequestEvent<TestPoolType>>.Publish(evt);
        }
    }
}

