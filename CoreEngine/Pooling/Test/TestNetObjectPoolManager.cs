using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Pool.Test
{
    public enum TestPoolType
    {
        Square_Scene,
        Circle_Global,
    }
    public class TestNetObjectPoolManager : BaseNetObjectPoolManager<TestPoolType>
    {
    }
}

