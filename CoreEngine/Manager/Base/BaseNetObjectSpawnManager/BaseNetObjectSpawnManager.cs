using CoreEngine.Facades;
using CoreEngine.Helpers;
using CoreEngine.Manager;
using CoreEngine.Manager.Pool;
using FishNet;
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

        // 틱 연산 완전 배제[cite: 8]
        protected override NetworkTickTarget networkTickTarget => NetworkTickTarget.None;

        private bool _isServerStarted;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _isServerStarted = true;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _isServerStarted = false;
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
            while (!_isServerStarted)
            {
                yield return null;
            }

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
                poolManager.Spawn(data.poolType, data.position, Quaternion.Euler(data.rotation));
            }

            LogHelper.Log($"[{this.GetType().Name}] {spawnDataList.Count}개의 인게임 객체 동적 스폰 완료!", LogColor.Green);
        }

        
    }
}