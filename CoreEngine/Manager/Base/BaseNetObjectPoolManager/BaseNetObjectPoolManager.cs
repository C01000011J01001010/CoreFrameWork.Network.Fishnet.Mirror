using CoreEngine.Manager.Pool;
using FishNet;
using System;
using UnityEngine;
using CoreEngine.Network.FishNetExtension.Manager.Pool;

namespace CoreEngine.Network.FishNetExtension.Manager
{
    /// <summary>
    /// 로컬 풀링 시스템을 상속받아 네트워크 스폰/디스폰 통제권을 결합한 매니저
    /// </summary>
    public abstract class BaseNetObjectPoolManager<TPoolType> : BasePoolManager<TPoolType, NetObjectPoolHandler<TPoolType>>
        where TPoolType : Enum
    {

    }
}