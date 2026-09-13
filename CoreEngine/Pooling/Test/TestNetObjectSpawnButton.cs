using UnityEngine;
using CoreEngine.Pool.Test;
using CoreEngine.Pool;
using CoreEngine.Facades;
using CoreEngine.EventBus;
using CoreEngine.Network.FishNetExtension.Spawn;

namespace CoreEngine.Network.FishNetExtension.Pool.Test
{
    public class TestNetObjectSpawnButton : BaseTestSpawnButton<TestPoolType>
    {

        protected override void OnClickSpawn()
        {
            Vector3 randomPos = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            Vector3 randomRot = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            RequestSpawnData<TestPoolType> _spawnData = new(targetPoolType, randomPos, randomRot);

            SpawnRequestEvent<TestPoolType> evt = new(_spawnData, false);
            EventBus<SpawnRequestEvent<TestPoolType>>.Publish(evt);
        }
    }
}

