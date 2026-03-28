using System.Collections.Generic;
using UnityEngine;

public class AugmentRunManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static AugmentRunManager Instance;

    [Header("최대 보유 가능한 증강 개수")]
    [SerializeField] private int maxAugmentCount = 5;

    // 현재 런에서 보유 중인 증강 목록
    private readonly List<AugmentationSystem> ownedAugments = new List<AugmentationSystem>();

    // 외부에서는 읽기만 가능하게 공개
    public IReadOnlyList<AugmentationSystem> OwnedAugments => ownedAugments;

    // 현재 몇 개 보유 중인지
    public int OwnedCount => ownedAugments.Count;

    // 더 먹을 수 있는지 여부
    public bool CanPickMore => ownedAugments.Count < maxAugmentCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasAugment(AugmentationSystem aug)
    {
        // 특정 증강을 이미 가지고 있는지 체크
        return ownedAugments.Contains(aug);
    }

    public bool TryAddAugment(AugmentationSystem aug)
    {
        // null이면 추가 불가
        if (aug == null) return false;

        // 최대 개수면 추가 불가
        if (!CanPickMore) return false;

        // 이미 가진 증강이면 중복 추가 불가
        if (ownedAugments.Contains(aug)) return false;

        // 정상적으로 추가
        ownedAugments.Add(aug);
        return true;
    }

    public void ResetRun()
    {
        // 런 초기화 시 보유 증강 전부 제거
        ownedAugments.Clear();

        // 증강 UI 슬롯도 같이 초기화
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }

        // 플레이어 스탯도 원래 상태로 다시 계산
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.RebuildPlayerStats();
        }
    }
}