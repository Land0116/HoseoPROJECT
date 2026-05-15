using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("테스트 스폰")]
    [SerializeField] private bool isSpawnSure = false;

    [Header("스테이지별 몬스터")]
    [SerializeField] private GameObject[] stage1Monster;
    [SerializeField] private GameObject[] stage2Monster;
    [SerializeField] private GameObject[] stage3Monster;

    [Header("몬스터 생성 위치")]
    [SerializeField] private Transform[] spawnPoints;

    private CombatRoomController combatRoomController;

    private readonly List<GameObject> aliveMonsters = new List<GameObject>();

    private bool isWaveActive = false;

    private void Awake()
    {
        AutoBind();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBind();
    }

    private void Reset()
    {
        AutoBind();
    }
#endif

    private void Update()
    {
        // 기존 테스트 스폰 유지
        if (isSpawnSure)
        {
            isSpawnSure = false;
            SpawnWave(1);
        }

        // 현재 웨이브가 진행 중이 아니면 검사하지 않음
        if (!isWaveActive) return;

        // Destroy된 몬스터 제거
        aliveMonsters.RemoveAll(monster => monster == null);

        // 살아있는 몬스터가 없으면 웨이브 종료 처리
        if (aliveMonsters.Count <= 0)
        {
            isWaveActive = false;

            if (combatRoomController != null)
            {
                combatRoomController.OnAllMonstersDead();
            }
        }
    }

    [ContextMenu("Auto Bind")]
    private void AutoBind()
    {
        if (combatRoomController == null)
        {
            combatRoomController = GetComponentInParent<CombatRoomController>();
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Transform spawnRoot = AutoBindUtility.FindChildRecursive(transform, "MonsterSpawnPoints");

            if (spawnRoot != null)
            {
                spawnPoints = new Transform[spawnRoot.childCount];

                for (int i = 0; i < spawnRoot.childCount; i++)
                {
                    spawnPoints[i] = spawnRoot.GetChild(i);
                }
            }
        }
    }

    public void SetSpawn(bool value)
    {
        if (!value) return;

        SpawnWave(1);
    }

    public void SpawnWave(int waveIndex)
    {
        aliveMonsters.Clear();

        int stage = GetCurrentStageNumber();
        GameObject[] selectedPool = GetStageMonsterPool(stage);

        if (selectedPool == null || selectedPool.Length == 0)
        {
            Debug.LogWarning($"스테이지 {stage} 몬스터 풀이 비어 있음");

            if (combatRoomController != null)
                combatRoomController.OnAllMonstersDead();

            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("몬스터 스폰 포인트가 없음. MonsterSpawner 위치에 1마리 생성");

            GameObject prefab = selectedPool[Random.Range(0, selectedPool.Length)];
            GameObject monster = Instantiate(prefab, transform.position, Quaternion.identity);

            aliveMonsters.Add(monster);
            isWaveActive = true;
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject prefab = selectedPool[Random.Range(0, selectedPool.Length)];

            if (prefab == null) continue;

            GameObject monster = Instantiate(prefab, spawnPoints[i].position, Quaternion.identity);
            aliveMonsters.Add(monster);
        }

        isWaveActive = true;
    }

    private int GetCurrentStageNumber()
    {
        // 새 맵 시스템 기준
        if (MapFlowManager.Instance != null)
        {
            return MapFlowManager.Instance.CurrentAct;
        }

        // 기존 StageClear가 아직 살아있을 경우
        if (StageClear.Instance != null)
        {
            return StageClear.Instance.GetCurrentStageNumber();
        }

        return 1;
    }

    private GameObject[] GetStageMonsterPool(int stage)
    {
        switch (stage)
        {
            case 1:
                return stage1Monster;

            case 2:
                return stage2Monster;

            case 3:
                return stage3Monster;
        }

        return stage1Monster;
    }
}