using CoreEngine.EventBus;
using CoreEngine.Facades;
using CoreEngine.Helpers;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.Network.FishNetExtension.Manager
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
    public struct DynamicSpawnRequestEvent<TPoolType> : IEvent
        where TPoolType : Enum
    {
        public RequestSpawnData<TPoolType> SpawnData;
        public bool IsOwner; // 소유를 주장할것인지 여부

        public DynamicSpawnRequestEvent(RequestSpawnData<TPoolType> spawnData, bool isOwner)
        {
            SpawnData = spawnData;
            IsOwner = isOwner;
        }
    }


    /// <summary>
    /// 풀링 시스템과 연동되어 씬 초기화 시 서버 권위 객체들을 스폰하는 제네릭 매니저
    /// </summary>
    public abstract class BaseNetObjectSpawnManager<TPoolType, TPoolManager> : BaseNetworkManager 
        where TPoolType : Enum
        where TPoolManager : BaseNetObjectPoolManager<TPoolType>
    {
        [Header("Spawn Settings")]
        [Tooltip("CSV 또는 에디터 기즈모를 통해 세팅된 초기 스폰 데이터")]
        public List<SpawnData<TPoolType>> spawnDataList = new List<SpawnData<TPoolType>>();

        [Header("Editor Visualization")]
        [Tooltip("씬 뷰에서 전체 스폰 데이터의 방향을 원뿔로 한눈에 표시합니다.")]
        public bool showAllCones = true;

        // 틱 연산 완전 배제
        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;


        RepeatEventConsumer<DynamicSpawnRequestEvent<TPoolType>> requestEvent;

        public override void Awake()
        {
            base.Awake();
            requestEvent = new RepeatEventConsumer<DynamicSpawnRequestEvent<TPoolType>>(OnDynamicSpawnRequested);
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            requestEvent.Bind();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            requestEvent.Unbind();
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

            // 100% 보장된 인프라 위에서 안전하게 풀링 및 스폰 실행
            SpawnAllEntities();
        }

        private void SpawnAllEntities()
        {
            // Facade를 통해 나와 동일한 Enum 타입을 쓰는 풀 매니저를 호출
            // Get모듈은 구체타입을 명시해야 하므로 제네릭 타입 TPoolManager를 그대로 전달
            var poolManager = CoreFacade.GetManager<TPoolManager>();

            if (poolManager == null)
            {
                LogHelper.Log($"[{this.GetType().Name}] 풀 매니저를 찾을 수 없습니다.", LogColor.Red);
                return;
            }

            foreach (var data in spawnDataList)
            {
                // 풀에서 객체를 꺼내고 FishNet 서버 권위로 스폰
                // BaseNetObjectPoolManager(NetObjectPoolHandler)가 FishNet 서버 권위로 Spawn을 처리하도록 설계되어 있으므로
                // 여기서는 단순히 풀에서 꺼내기만 하면 됨
                poolManager.Spawn(data.poolType, data.position, Quaternion.Euler(data.rotation)); // 정적 스폰은 부모를 설정할 수 없음
            }

            LogHelper.Log($"[{this.GetType().Name}] {spawnDataList.Count}개의 인게임 객체 동적 스폰 완료!", LogColor.Green);
        }

        private void OnDynamicSpawnRequested(DynamicSpawnRequestEvent<TPoolType> evt)
        {
            if (InstanceFinder.IsOffline) return;

            // 객체를 통째로 넘기지 않고 안전한 기본 타입들만 분해해서 전송
            int poolTypeInt = Convert.ToInt32(evt.SpawnData.poolType);
            RequestSpawnServerRpc(poolTypeInt, evt.SpawnData.position, Quaternion.Euler( evt.SpawnData.rotation), evt.IsOwner, evt.SpawnData.parentNetObj);
        }

        // RequireOwnership = false: 이 매니저의 주인이 아니어도 호출 가능
        // NetworkConnection caller: 호출한 클라이언트가 누구인지 FishNet이 자동 식별
        [ServerRpc(RequireOwnership = false)]
        private void RequestSpawnServerRpc(int poolTypeInt, Vector3 position, Quaternion rotation, bool isowner, NetworkObject parentNetObj, NetworkConnection caller = null)
        {
            StartCoroutine(DynamicSpawn(poolTypeInt,position,rotation,isowner,parentNetObj, caller));
        }

        private IEnumerator DynamicSpawn(int poolTypeInt, Vector3 position, Quaternion rotation, bool isowner, NetworkObject parentNetObj, NetworkConnection caller)
        {
            while (!IsServerStarted) yield return null;

            var poolManager = CoreFacade.GetManager<TPoolManager>();
            if (poolManager == null) yield break;

            // 데이터 복구
            TPoolType poolType = (TPoolType)Enum.ToObject(typeof(TPoolType), poolTypeInt);

            Transform parent = null;
            // 부모로 설정할 Caller의 네트워크 객체가 있다면 설정
            if (parentNetObj != null && parentNetObj.Owner == caller)
            {
                parent = parentNetObj.transform;
            }

            // 서버가 풀에서 객체를 꺼냄
            var pObj = poolManager.Spawn(poolType, position, rotation, parent);

            // 꺼낸 객체가 네트워크 객체인지 확인
            if (pObj.TryGetComponent(out NetworkObject networkObject))
            {
                // 소유를 주장한다면 소유권 부여
                if (isowner && caller != null && caller.IsValid)
                {
                    networkObject.GiveOwnership(caller);
                }
                // 이후 부모 위치를 바꾸는 것은 Client에서 자발적으로 바꾸고 서버에게 알리도록 함
            }
        }
    }
}