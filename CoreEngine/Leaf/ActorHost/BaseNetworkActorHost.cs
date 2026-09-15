using CoreEngine.Actor;
using CoreEngine.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension
{
    public abstract class BaseNetworkActorHost : BaseNetworkActor, IActorHost
    {
        protected readonly FeatureHandler FeatureHandler = new();

        public override void Awake()
        {
            base.Awake();
            FeatureHandler.SetHost(this);
        }

        public bool TryGetFeature<T>(out T feature) where T : class, IActorFeature
        {
            return FeatureHandler.TryGetFeature(out feature);
        }
    }
}
