using System;
using UnityEngine;

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

    public void SpawnRewardObject(RewardType rewardType, Action onRewardFinished)
    {
        if (rewardObjectPrefab == null)
        {
            Debug.LogWarning("[RewardObjectSpawner] RewardObjectPrefab이 없음. 보상 완료 처리.");
            onRewardFinished?.Invoke();
            return;
        }

        Vector3 spawnPosition = GetRewardSpawnPosition();

        RewardInteractObject rewardObject = Instantiate(
            rewardObjectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Sprite icon = GetIcon(rewardType);

        rewardObject.Setup(rewardType, icon, onRewardFinished);
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