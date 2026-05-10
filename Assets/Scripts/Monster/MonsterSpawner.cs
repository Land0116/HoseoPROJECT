using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("���� Ʈ����")]
    [SerializeField] private bool isSpawnSure = false;

    [Header("���������� ����")]
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

        if (!value)
            return;

        hasSpawned = false;

        SpawnMonster();

        hasSpawned = true;
        isSpawnSure = false;
    }

    private void SpawnMonster()
    {
        if (StageClear.Instance == null)
        {
            Debug.LogWarning("StageClear ����");
            return;
        }

        int stage = StageClear.Instance.GetCurrentStageNumber();

        GameObject[] selectedPool = GetStageMonsterPool(stage);

        if (selectedPool == null || selectedPool.Length == 0)
        {
            Debug.LogWarning($"�������� {stage} ���� ����");
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