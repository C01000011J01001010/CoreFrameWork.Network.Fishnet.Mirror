using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Pool.Test
{
    public enum TestPoolType
    {
        Square,
        Circle,
    }
    public class TestNetObjectPoolManager : BaseNetObjectPoolManager<TestPoolType>
    {
    }
}

