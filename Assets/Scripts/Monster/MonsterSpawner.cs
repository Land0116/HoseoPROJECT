using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 트리거")]
    [SerializeField] private bool isSpawnSure = false;

    [Header("스테이지별 몬스터")]
    [SerializeField] private GameObject[] stage1Monster;
    [SerializeField] private GameObject[] stage2Monster;
    [SerializeField] private GameObject[] stage3Monster;

    private bool hasSpawned = false;

    private void Update()
    {
        if (!isSpawnSure) return;
        if (hasSpawned) return;

        SpawnMonster();
        hasSpawned = true;
        isSpawnSure = false;
    }

    public void SetSpawn(bool value)
    {
        isSpawnSure = value;

        if (value)
        {
            hasSpawned = false;
        }
    }

    private void SpawnMonster()
    {
        if (StageClear.Instance == null)
        {
            Debug.LogWarning("StageClear 없음");
            return;
        }

        int stage = StageClear.Instance.GetCurrentStageNumber();

        GameObject[] selectedPool = GetStageMonsterPool(stage);

        if (selectedPool == null || selectedPool.Length == 0)
        {
            Debug.LogWarning($"스테이지 {stage} 몬스터 없음");
            return;
        }

        GameObject prefab = selectedPool[Random.Range(0, selectedPool.Length)];

        Instantiate(prefab, transform.position, Quaternion.identity);
    }

    private GameObject[] GetStageMonsterPool(int stage)
    {
        switch (stage)
        {
            case 1: return stage1Monster;
            case 2: return stage2Monster;
            case 3: return stage3Monster;
        }

        return stage1Monster; 
    }
}