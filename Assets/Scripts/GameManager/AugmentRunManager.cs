using UnityEngine;

public class AugmentRunManager : MonoBehaviour
{
    public static AugmentRunManager Instance;

    #region Fields

    private const int MaxSlotCapacity = 12;

    [Header("슬롯 설정")]
    [SerializeField] private int baseSlotCount = 6;
    [SerializeField] private int currentSlotCount = 6;

    [Header("현재 런 슬롯 데이터")]
    [SerializeField] private AugmentSlotData[] ownedSlots = new AugmentSlotData[MaxSlotCapacity];

    #endregion

    #region Property

    public int CurrentSlotCount => currentSlotCount;
    public AugmentSlotData[] OwnedSlots => ownedSlots;

    #endregion

    #region Unity

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSlotCount = Mathf.Clamp(baseSlotCount, 1, MaxSlotCapacity);

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] == null)
                ownedSlots[i] = new AugmentSlotData();
        }
    }

    #endregion

    #region Public

    /// <summary>
    /// 해당 증강이 현재 슬롯 규칙상 선택 가능 상태인지 판단
    /// </summary>
    public bool CanOfferAugment(AugmentationSystem aug)
    {
        if (aug == null) return false;
        if (!aug.isUnlocked) return false;

        switch (aug.category)
        {
            case AugmentationSystem.AugmentCategory.SubSkill:
                return CanOfferSubSkill(aug);

            case AugmentationSystem.AugmentCategory.Passive:
                return CanOfferPassive(aug);

            case AugmentationSystem.AugmentCategory.Special:
                return CanOfferSpecial(aug);
        }

        return false;
    }

    /// <summary>
    /// 선택된 증강을 실제 슬롯에 반영
    /// </summary>
    public bool TryAddAugment(AugmentationSystem aug)
    {
        if (aug == null) return false;

        switch (aug.category)
        {
            case AugmentationSystem.AugmentCategory.SubSkill:
                return TryAddSubSkill(aug);

            case AugmentationSystem.AugmentCategory.Passive:
                return TryAddPassive(aug);

            case AugmentationSystem.AugmentCategory.Special:
                return TryAddSpecial(aug);
        }

        return false;
    }

    public void ResetRun()
    {
        currentSlotCount = Mathf.Clamp(baseSlotCount, 1, MaxSlotCapacity);

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] != null)
                ownedSlots[i].Clear();
        }

        NotifyChanged();
    }

    #endregion

    #region Offer Check

    private bool CanOfferSubSkill(AugmentationSystem aug)
    {
        int sameSkillIndex = FindSameSubSkillByID(aug.augmentID);
        if (sameSkillIndex >= 0)
        {
            return ownedSlots[sameSkillIndex].currentLevel < aug.maxLevel;
        }

        int sameTypeIndex = FindSameSubSkillByType(aug.subSkillType);
        if (sameTypeIndex >= 0)
        {
            return true; // 동일 타입이면 교체 가능
        }

        return FindFirstEmptySlotIndex() >= 0;
    }

    private bool CanOfferPassive(AugmentationSystem aug)
    {
        int sameEffectIndex = FindSamePassiveByType(aug.passiveType);
        if (sameEffectIndex >= 0)
        {
            return ownedSlots[sameEffectIndex].stackCount < aug.maxLevel;
        }

        return FindFirstEmptySlotIndex() >= 0;
    }

    private bool CanOfferSpecial(AugmentationSystem aug)
    {
        if (HasSpecial(aug.augmentID))
            return false;

        return FindFirstEmptySlotIndex() >= 0;
    }

    #endregion

    #region Add Logic

    private bool TryAddSubSkill(AugmentationSystem aug)
    {
        // 1. 동일 스킬이면 레벨 증가
        int sameSkillIndex = FindSameSubSkillByID(aug.augmentID);
        if (sameSkillIndex >= 0)
        {
            if (ownedSlots[sameSkillIndex].currentLevel >= aug.maxLevel)
                return false;

            ownedSlots[sameSkillIndex].currentLevel++;
            NotifyChanged();
            return true;
        }

        // 2. 동일 타입이면 기존 스킬 교체
        int sameTypeIndex = FindSameSubSkillByType(aug.subSkillType);
        if (sameTypeIndex >= 0)
        {
            ownedSlots[sameTypeIndex].Set(aug);
            NotifyChanged();
            return true;
        }

        // 3. 다른 타입이면 빈 슬롯 추가
        int emptyIndex = FindFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        ownedSlots[emptyIndex].Set(aug);
        NotifyChanged();
        return true;
    }

    private bool TryAddPassive(AugmentationSystem aug)
    {
        // 동일 효과면 중첩
        int sameEffectIndex = FindSamePassiveByType(aug.passiveType);
        if (sameEffectIndex >= 0)
        {
            if (ownedSlots[sameEffectIndex].stackCount >= aug.maxLevel)
                return false;

            ownedSlots[sameEffectIndex].stackCount++;
            NotifyChanged();
            return true;
        }

        // 다른 효과면 추가
        int emptyIndex = FindFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        ownedSlots[emptyIndex].Set(aug);
        NotifyChanged();
        return true;
    }

    private bool TryAddSpecial(AugmentationSystem aug)
    {
        if (HasSpecial(aug.augmentID))
            return false;

        int emptyIndex = FindFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        ownedSlots[emptyIndex].Set(aug);
        NotifyChanged();
        return true;
    }

    #endregion

    #region Find

    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < currentSlotCount; i++)
        {
            if (ownedSlots[i] == null) continue;
            if (!ownedSlots[i].isOccupied) return i;
        }

        return -1;
    }

    private int FindSameSubSkillByID(string augmentID)
    {
        if (string.IsNullOrEmpty(augmentID)) return -1;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;
            if (slot.augmentData.category != AugmentationSystem.AugmentCategory.SubSkill) continue;

            if (slot.augmentData.augmentID == augmentID)
                return i;
        }

        return -1;
    }

    private int FindSameSubSkillByType(AugmentationSystem.SubSkillType subSkillType)
    {
        if (subSkillType == AugmentationSystem.SubSkillType.None) return -1;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;
            if (slot.augmentData.category != AugmentationSystem.AugmentCategory.SubSkill) continue;

            if (slot.augmentData.subSkillType == subSkillType)
                return i;
        }

        return -1;
    }

    private int FindSamePassiveByType(AugmentationSystem.PassiveType passiveType)
    {
        if (passiveType == AugmentationSystem.PassiveType.None) return -1;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;
            if (slot.augmentData.category != AugmentationSystem.AugmentCategory.Passive) continue;

            if (slot.augmentData.passiveType == passiveType)
                return i;
        }

        return -1;
    }

    private bool HasSpecial(string augmentID)
    {
        if (string.IsNullOrEmpty(augmentID)) return false;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;
            if (slot.augmentData.category != AugmentationSystem.AugmentCategory.Special) continue;

            if (slot.augmentData.augmentID == augmentID)
                return true;
        }

        return false;
    }
    
    public int GetPreviewLevel(AugmentationSystem aug)
    {
        if (aug == null) return 1;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null) continue;
            if (!slot.isOccupied) continue;
            if (slot.augmentData == null) continue;

            // 같은 증강이면 다음 레벨 미리보기
            if (slot.augmentData.augmentID == aug.augmentID)
            {
                if (aug.category == AugmentationSystem.AugmentCategory.SubSkill)
                {
                    return Mathf.Clamp(slot.currentLevel + 1, 1, aug.maxLevel);
                }

                if (aug.category == AugmentationSystem.AugmentCategory.Passive)
                {
                    return Mathf.Clamp(slot.stackCount + 1, 1, aug.maxLevel);
                }

                if (aug.category == AugmentationSystem.AugmentCategory.Special)
                {
                    return 1;
                }
            }

            // 패시브형은 동일 효과 중첩이니까 같은 passiveType도 체크
            if (aug.category == AugmentationSystem.AugmentCategory.Passive &&
                slot.augmentData.category == AugmentationSystem.AugmentCategory.Passive &&
                slot.augmentData.passiveType == aug.passiveType)
            {
                return Mathf.Clamp(slot.stackCount + 1, 1, aug.maxLevel);
            }
        }

        // 처음 먹는 증강이면 Lv1
        return 1;
    }

    #endregion

    #region Utility

    private void NotifyChanged()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.RebuildPlayerStats();
        }

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }

    #endregion
}