using CoreEngine.EventBus;
using CoreEngine.Facades;
using CoreEngine.Helpers;
using CoreEngine.Network.FishNetExtension.Pool;
using CoreEngine.Pool;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Spawn
{
    [Serializable]
    public class SpawnData<TPoolType> where TPoolType : Enum
    {
        public TPoolType poolType; // 프로젝트마다 달라질 수 있는 스폰 대상
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    public class RequestSpawnData<TPoolType> : SpawnData<TPoolType>
    where TPoolType : Enum
    {
        public NetworkObject parentNetObj;

        public RequestSpawnData() { }
        public RequestSpawnData(
            TPoolType poolType,
            Vector3 position,
            Vector3 rotation,
            NetworkObject parentNetObj = null)
        {
            this.poolType = poolType;
            this.position = position;
            this.rotation = rotation;
            this.parentNetObj = parentNetObj;
        }
    }
    // 누군가 동적 스폰을 원할 때 허공에 던지는 이벤트
    public struct SpawnRequestEvent<TPoolType> : IEvent
        where TPoolType : Enum
    {
        public RequestSpawnData<TPoolType> SpawnData;
        public bool IsOwner; // 소유를 주장할것인지 여부
        public SpawnRequestEvent(RequestSpawnData<TPoolType> spawnData, bool isOwner = false)
        {
            SpawnData = spawnData;
            IsOwner = isOwner;
        }
    }

    public struct DespawnRequestEvent : IEvent
    {
        public NetworkObject networkObject;
        public DespawnRequestEvent(NetworkObject networkObject)
        {
            this.networkObject  = networkObject;
        }
    }


    /// <summary>
    /// 풀링 시스템과 연동되어 씬 초기화 시 서버 권위 객체들을 스폰하는 제네릭 매니저
    /// </summary>
    public abstract class BaseNetObjectSpawnManager<TPoolType, TPoolManager> : BaseNetSpawnBridgeManager
        where TPoolType : Enum
        where TPoolManager : BaseNetObjectPoolManager<TPoolType>
    {
        [Header("Spawn Settings")]
        [Tooltip("CSV 또는 에디터 기즈모를 통해 세팅된 초기 스폰 데이터")]
        public List<SpawnData<TPoolType>> spawnDataList = new List<SpawnData<TPoolType>>();

        [Header("Editor Visualization")]
        [Tooltip("씬 뷰에서 전체 스폰 데이터의 방향을 원뿔로 한눈에 표시합니다.")]
        public bool showAllCones = true;

        TPoolManager _poolManager;

        // 틱 연산 완전 배제
        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus<SpawnRequestEvent<TPoolType>>.Subscribe(OnSpawnRequested);

        }
        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<SpawnRequestEvent<TPoolType>>.Unsubscribe(OnSpawnRequested);
        }


        public override IEnumerator Initialize()
        {
            yield return base.Initialize();

            // 서버 권한 없이 순수 클라이언트로 접속한 경우 스폰 로직을 즉시 탈출
            if (InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
            {
                yield break;
            }

            // FishNet 서버가 완전히 올라올 때까지 대기
            while (!IsServerStarted) yield return null;

            // 안전하게 풀링 및 스폰 실행
            SpawnAllEntities();
        }

        private void SpawnAllEntities()
        {
            if (!TryGetPoolManager()) return ;

            foreach (var data in spawnDataList)
            {
                IPoolable pObj = _poolManager.Spawn(data.poolType, data.position, Quaternion.Euler(data.rotation));
                if(pObj is NetworkBehaviour netBehaviour)
                {
                    ServerManager.Spawn(netBehaviour.NetworkObject);
                }
            }

            LogHelper.Log($"[{this.GetType().Name}] {spawnDataList.Count}개의 인게임 객체 동적 스폰 완료!", LogColor.Green);
        }

        // -------------------------------------------------------------------------------------

        private void OnSpawnRequested(SpawnRequestEvent<TPoolType> evt)
        {
            if (InstanceFinder.IsOffline) return;

            int poolTypeInt = Convert.ToInt32(evt.SpawnData.poolType);
            // 부모의 비제네릭 ServerRpc 호출
            RequestSpawnServerRpc(poolTypeInt, evt.SpawnData.position, evt.SpawnData.rotation, evt.IsOwner, evt.SpawnData.parentNetObj);
        }

        // [ServerRpc] 속성 제거: RPC는 비제네릭 부모가 받고, 실제 처리는 여기서 오버라이드
        protected override void OnServerSpawnRequested(int poolTypeInt, Vector3 position, Vector3 rotation, bool isOwner, NetworkObject parentNetObj, NetworkConnection caller)
        {
            if (isOwner) isOwner = AllowSpawnOwnership();
            StartCoroutine(DynamicSpawn(poolTypeInt, position, rotation, isOwner, parentNetObj, caller));
        }

        /// <summary>
        /// Client가 Spawn 요청 시 ownership을 주장했을 때 어떻게 처리할것인지 결정
        /// </summary>
        protected virtual bool AllowSpawnOwnership(){ return true; }

        private IEnumerator DynamicSpawn(int poolTypeInt, Vector3 position, Vector3 rotation, bool isOwner, NetworkObject parentNetObj, NetworkConnection caller)
        {
            while (!base.IsServerStarted) yield return null;
            if (!TryGetPoolManager()) yield break;

            if (!Enum.IsDefined(typeof(TPoolType), poolTypeInt))
            {
                LogHelper.Log($"[{GetType().Name}] 잘못된 PoolType 요청: {poolTypeInt}",LogColor.Red);
                yield break;
            }

            TPoolType poolType = (TPoolType)Enum.ToObject(typeof(TPoolType), poolTypeInt);
            Transform parent = null;
            if (parentNetObj != null && parentNetObj.Owner == caller)
            {
                parent = parentNetObj.transform;
            }

            IPoolable pObj = _poolManager.Spawn(poolType, position, Quaternion.Euler(rotation), parent);
            if (pObj is NetworkBehaviour netBehaviour)
            {
                ServerManager.Spawn(netBehaviour.NetworkObject, isOwner ? caller : null);

                if(isOwner && caller == null)
                {
                    LogHelper.LogWarning($"Spawn 요청자의 소유권이 인정됐으나, caller가 null");
                }
            }
        }


        private bool TryGetPoolManager()
        {
            if (_poolManager != null)
                return true;

            _poolManager = CoreFacade.GetManager<TPoolManager>();

            if (_poolManager == null)
            {
                LogHelper.Log(
                    $"[{GetType().Name}] 풀 매니저를 찾을 수 없습니다.",
                    LogColor.Red);

                return false;
            }

            return true;
        }
    }
}