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

        /*
         * 보스가 죽었을 때 BossRoomController.OnBossDead()를 호출해야 함.
         *
         * 예:
         * FindObjectOfType<BossRoomController>().OnBossDead();
         *
         * 더 좋은 방식은 보스 HP 스크립트에서 이벤트로 연결하는 것.
         */
    }
}