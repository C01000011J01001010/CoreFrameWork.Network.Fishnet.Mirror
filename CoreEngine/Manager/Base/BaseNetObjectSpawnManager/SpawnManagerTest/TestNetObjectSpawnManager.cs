using UnityEngine;
namespace CoreEngine.Network.FishNetExtension.Spawn.Test
{
    public enum TestPoolType
    {
        Enemy,
        Ally,
        Neutral
    }
    public class TestNetObjectSpawnManager : BaseNetObjectSpawnManager<TestPoolType, TestNetworkPoolManager>
    {

    }
}

