using UnityEngine;
namespace CoreEngine.Network.FishNetExtension.Manager.NetObjectSpawn.Test
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

