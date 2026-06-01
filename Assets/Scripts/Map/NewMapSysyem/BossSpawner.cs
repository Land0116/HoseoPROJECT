using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("보스 프리팹")]
    [SerializeField] private GameObject bossPrefab;

    [Header("보스방 컨트롤러")]
    [SerializeField] private BossRoomController bossRoomController;

    [Header("스폰 위치")]
    [SerializeField] private Transform spawnPoint;

    private Monster currentBoss;
    private bool bossDeadNotified;

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

    private void AutoBind()
    {
        if (bossRoomController == null)
        {
            bossRoomController = GetComponentInParent<BossRoomController>();
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }
    public bool ShouldDropBossReward()
    {
        if (bossRoomController == null)
            return true;

        return !bossRoomController.IsFinalBossRoom();
    }

    public void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("[BossSpawner] bossPrefab이 없음");
            return;
        }

        bossDeadNotified = false;

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);

        currentBoss = bossObject.GetComponent<Monster>();

        if (currentBoss != null)
        {
            currentBoss.SetBossSpawner(this);
        }
        else
        {
            Debug.LogWarning("[BossSpawner] 보스 프리팹에 Monster 컴포넌트가 없음");
        }
    }

    public void NotifyBossDead()
    {
        if (bossDeadNotified)
            return;

        bossDeadNotified = true;

        Debug.Log("[BossSpawner] 보스 사망 알림 받음");

        if (bossRoomController != null)
        {
            bossRoomController.OnBossDead();
        }
        else
        {
            Debug.LogWarning("[BossSpawner] bossRoomController가 없음");
        }
    }
}