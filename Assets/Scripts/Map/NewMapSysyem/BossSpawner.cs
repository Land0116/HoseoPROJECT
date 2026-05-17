using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("보스 프리팹")]
    [SerializeField] private GameObject bossPrefab;

    [Header("보스 생성 위치")]
    [SerializeField] private Transform spawnPoint;

    private BossRoomController bossRoomController;

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

    [ContextMenu("Auto Bind")]
    private void AutoBind()
    {
        if (bossRoomController == null)
        {
            bossRoomController = GetComponentInParent<BossRoomController>();
        }

        if (spawnPoint == null)
        {
            Transform found = AutoBindUtility.FindChildRecursive(transform, "BossSpawnPoint");

            if (found != null)
            {
                spawnPoint = found;
            }
        }
    }

    public void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("BossPrefab이 없음");
            return;
        }

        Vector3 pos = transform.position;

        if (spawnPoint != null)
        {
            pos = spawnPoint.position;
        }

        GameObject boss = Instantiate(bossPrefab, pos, Quaternion.identity);

        Monster bossMonster = boss.GetComponent<Monster>();

        if (bossMonster == null)
        {
            bossMonster = boss.GetComponentInChildren<Monster>();
        }

        if (bossMonster != null)
        {
            bossMonster.SetBossSpawner(this);
        }
        else
        {
            Debug.LogWarning("[BossSpawner] 생성된 보스 프리팹에 Monster 컴포넌트가 없음");
        }
    }

    public void NotifyBossDead()
    {
        if (bossRoomController != null)
        {
            bossRoomController.OnBossDead();
        }
        else
        {
            BossRoomController found = FindAnyObjectByType<BossRoomController>();

            if (found != null)
            {
                found.OnBossDead();
            }
        }
    }
}