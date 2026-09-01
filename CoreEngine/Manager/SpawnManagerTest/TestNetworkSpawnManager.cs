using UnityEngine;
namespace CoreEngine.Network.FishNetExtension.Manager.Test
{
    public enum TestPoolType
    {
        Enemy,
        Ally,
        Neutral
    }
    public class TestNetworkSpawnManager : NetworkSpawnManager<TestPoolType, TestNetworkPoolManager>
    {

    }
}

