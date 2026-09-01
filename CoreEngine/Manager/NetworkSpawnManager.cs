using CoreEngine.EventBus;
using CoreEngine.Facades;
using CoreEngine.Manager;
using FishNet.Object;
using System;
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

    /// <summary>
    /// 풀링 시스템과 연동되어 씬 초기화 시 서버 권위 객체들을 스폰하는 제네릭 매니저
    /// </summary>
    public abstract class NetworkSpawnManager<TPoolType> : BaseNetworkManager where TPoolType : Enum
    {
        [Header("Spawn Settings")]
        [Tooltip("CSV 또는 에디터 기즈모를 통해 세팅된 초기 스폰 데이터")]
        public List<SpawnData<TPoolType>> spawnDataList = new List<SpawnData<TPoolType>>();

        [Header("Editor Visualization")]
        [Tooltip("씬 뷰에서 전체 스폰 데이터의 방향을 원뿔로 한눈에 표시합니다.")]
        public bool showAllCones = true;

        // 틱 연산 완전 배제[cite: 8]
        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

        public override void OnStartServer()
        {
            base.OnStartServer();
            EventBus<SceneReadyEvent>.Subscribe(OnSceneReady); 
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            EventBus<SceneReadyEvent>.Unsubscribe(OnSceneReady); 
        }

        private void OnSceneReady(SceneReadyEvent evt)
        {
            // Facade를 통해 나와 동일한 Enum 타입을 쓰는 풀 매니저를 호출
            var poolManager = CoreFacade.GetManager<ObjectPoolManager<TPoolType>>();

            if (poolManager == null)
            {
                Debug.LogError($"[{this.GetType().Name}] 풀 매니저를 찾을 수 없습니다.");
                return;
            }

            foreach (var data in spawnDataList)
            {
                // 풀에서 객체를 꺼내고 FishNet 서버 권위로 스폰
                GameObject obj = poolManager.Spawn(data.poolType, data.position);
                if (obj != null)
                {
                    obj.transform.eulerAngles = data.rotation;
                    base.ServerManager.Spawn(obj);
                }
            }

            Debug.Log($"[{this.GetType().Name}] {spawnDataList.Count}개의 인게임 객체 동적 스폰 완료!");
        }
    }
}