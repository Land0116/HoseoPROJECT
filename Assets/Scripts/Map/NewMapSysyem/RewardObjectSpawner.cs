using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RewardObjectSpawner : MonoBehaviour
{
    [Header("플레이어 위치 기준 보상 생성 오프셋")]
    [SerializeField] private Vector3 playerSpawnOffset = new Vector3(0f, 1.2f, 0f);

    [Header("공통 보상 오브젝트 프리팹")]
    [SerializeField] private RewardInteractObject rewardObjectPrefab;

    [Header("보상 아이콘")]
    [SerializeField] private Sprite augmentIcon;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Sprite itemIcon;
    
    [Header("보스 추가 체력 아이템 드랍")]
    [SerializeField] private GameObject bossHealthItemPrefab;

    [SerializeField, Range(0f, 1f)]
    private float bossHealthItemDropChance = 0.1f;

    [SerializeField] private Vector3 bossHealthItemSpawnOffset = new Vector3(0.6f, 0f, 0f);
    public void TrySpawnBossHealthItem(Vector3 bossPosition)
    {
        if (bossHealthItemPrefab == null)
        {
            Debug.LogWarning("[RewardObjectSpawner] bossHealthItemPrefab이 없음. 체력 아이템 드랍 생략.");
            return;
        }

        if (Random.value > bossHealthItemDropChance)
            return;

        Vector3 spawnPosition = bossPosition + bossHealthItemSpawnOffset;

        Instantiate(
            bossHealthItemPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Debug.Log("[RewardObjectSpawner] 보스 추가 체력 아이템 드랍 성공");
    }

    public void SpawnRewardObject(RewardType rewardType, Action onRewardFinished)
    {
        SpawnRewardObjectAtPosition(
            rewardType,
            GetRewardSpawnPosition(),
            onRewardFinished
        );
    }

    public void SpawnRewardObjectAtPosition(
        RewardType rewardType,
        Vector3 spawnPosition,
        Action onRewardFinished)
    {
        if (rewardObjectPrefab == null)
        {
            Debug.LogWarning("[RewardObjectSpawner] RewardObjectPrefab이 없음. 보상 완료 처리.");
            onRewardFinished?.Invoke();
            return;
        }

        RewardInteractObject rewardObject = Instantiate(
            rewardObjectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Sprite icon = GetIcon(rewardType);

        rewardObject.Setup(rewardType, icon, onRewardFinished);

        FieldRewardDropMotion dropMotion = rewardObject.GetComponent<FieldRewardDropMotion>();

        if (dropMotion != null)
        {
            dropMotion.Play();
        }
    }

    public RewardType GetRandomBossRewardType()
    {
        return Random.value < 0.8f
            ? RewardType.Augment
            : RewardType.Skill;
    }

    private Vector3 GetRewardSpawnPosition()
    {
        if (PlayerController.Instance != null)
        {
            return PlayerController.Instance.transform.position + playerSpawnOffset;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            return playerObject.transform.position + playerSpawnOffset;
        }

        Debug.LogWarning("[RewardObjectSpawner] Player를 찾지 못해서 RewardObjectSpawner 위치에 보상 생성.");
        return transform.position;
    }

    private Sprite GetIcon(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.Augment:
                return augmentIcon;

            case RewardType.Skill:
                return skillIcon;

            case RewardType.Item:
                return itemIcon;

            default:
                return null;
        }
    }
}