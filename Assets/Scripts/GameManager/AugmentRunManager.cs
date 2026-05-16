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
            Destroy(this);
            return;
        }

        Instance = this;

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
        if (aug == null) 
            return false;

        if (!CanOfferAugment(aug))
            return false;
        
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

        // 같은 증강 레벨업은 허용
        if (sameSkillIndex >= 0)
        {
            return ownedSlots[sameSkillIndex].currentLevel < aug.maxLevel;
        }

        // 주위탄 조합 금지 규칙
        if (IsBlockedByOrbitRule(aug))
            return false;

        // 같은 계열이면 교체 가능
        int sameTypeIndex = FindSameSubSkillForReplacement(aug);
        if (sameTypeIndex >= 0)
        {
            return true;
        }

        return FindFirstEmptySlotIndex() >= 0;
    }

    private bool CanOfferPassive(AugmentationSystem aug)
    {
        if (IsBlockedByOrbitRule(aug))
            return false;

        int sameEffectIndex = FindSamePassiveByType(aug.passiveType);
        if (sameEffectIndex >= 0)
        {
            return ownedSlots[sameEffectIndex].stackCount < aug.maxLevel;
        }

        return FindFirstEmptySlotIndex() >= 0;
    }

    private bool CanOfferSpecial(AugmentationSystem aug)
    {
        if (aug == null) return false;
        if (aug.specialType == AugmentationSystem.SpecialType.None) return false;

        // 같은 증강 ID 금지
        if (HasSpecial(aug.augmentID))
            return false;

        // 같은 스페셜 종류 금지
        if (HasSpecialType(aug.specialType))
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
        int sameTypeIndex = FindSameSubSkillForReplacement(aug);
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
        if (aug == null) return false;
        if (aug.specialType == AugmentationSystem.SpecialType.None) return false;

        if (HasSpecial(aug.augmentID))
            return false;

        if (HasSpecialType(aug.specialType))
            return false;

        int emptyIndex = FindFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        ownedSlots[emptyIndex].Set(aug);
        NotifyChanged();
        return true;
    }
    
    /// <summary>
    /// 이미 같은 SpecialType의 스페셜 증강을 보유 중인지 확인한다.
    /// 
    /// 예:
    /// 이미 CheatDeath를 보유 중이면
    /// 다른 CheatDeath 계열 스페셜은 등장하지 않는다.
    /// </summary>
    private bool HasSpecialType(AugmentationSystem.SpecialType specialType)
    {
        if (specialType == AugmentationSystem.SpecialType.None)
            return false;

        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];

            if (slot == null) continue;
            if (!slot.isOccupied) continue;
            if (slot.augmentData == null) continue;
            if (slot.augmentData.category != AugmentationSystem.AugmentCategory.Special) continue;

            if (slot.augmentData.specialType == specialType)
                return true;
        }

        return false;
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

    /// <summary>
    /// 서브 스킬 증강 중 "서로 교체되어야 하는 슬롯"을 찾는다.
    /// 
    /// 핵심 규칙:
    /// 1. 일반 발사형 Shot끼리는 교체된다.
    ///    예: 3연발을 먹은 상태에서 차지샷을 먹으면 발사 방식이 교체됨.
    /// 
    /// 2. 주위탄 Shot끼리는 교체된다.
    ///    예: 주위탄 Lv1 계열을 다른 주위탄 계열로 바꾸는 경우.
    /// 
    /// 3. 일반 발사형 Shot과 주위탄 Shot은 서로 교체되지 않는다.
    ///    예: 3연발 + 주위탄 조합 가능.
    /// 
    /// 4. Trajectory / Effect는 서로 교체하지 않는다.
    ///    예: 유도 + 반사, 폭발 + 화상 조합 가능.
    /// 
    /// 최적화:
    /// - LINQ 사용 안 함.
    /// - 슬롯 배열만 for문으로 순회.
    /// </summary>
    private int FindSameSubSkillForReplacement(AugmentationSystem aug)
    {
        if (aug == null) return -1;

        if (aug.subSkillType == AugmentationSystem.SubSkillType.Shot)
        {
            for (int i = 0; i < currentSlotCount; i++)
            {
                AugmentSlotData slot = ownedSlots[i];

                if (slot == null) continue;
                if (!slot.isOccupied) continue;
                if (slot.augmentData == null) continue;

                AugmentationSystem ownedAug = slot.augmentData;

                if (ownedAug.category != AugmentationSystem.AugmentCategory.SubSkill)
                    continue;

                if (ownedAug.subSkillType != AugmentationSystem.SubSkillType.Shot)
                    continue;

                // 같은 발사 세부 타입끼리만 교체
                if (ownedAug.shotModifierType == aug.shotModifierType)
                    return i;
            }

            return -1;
        }

        // Trajectory / Effect는 교체하지 않는다.
        // 같은 타입이어도 조합 가능해야 하기 때문.
        return -1;
    }

    private bool IsBlockedByOrbitRule(AugmentationSystem candidate)
    {
        if (candidate == null) return true;

        bool candidateIsOrbit = IsOrbitShotAugment(candidate);
        bool ownedHasOrbit = HasOrbitShotAugment();
        bool candidateIsOrbitIncompatible = IsOrbitIncompatibleAugment(candidate);

        // 주위탄을 새로 뽑으려는데 이미 차지/반사/관통/연쇄/자동연사가 있으면 금지
        if (candidateIsOrbit && HasOrbitIncompatibleOwnedAugment())
            return true;

        // 이미 주위탄이 있는데 차지/반사/관통/연쇄/자동연사를 뽑으려 하면 금지
        if (ownedHasOrbit && candidateIsOrbitIncompatible)
            return true;

        return false;
    }

    private bool HasOrbitShotAugment()
    {
        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;

            if (IsOrbitShotAugment(slot.augmentData))
                return true;
        }

        return false;
    }

    private bool HasOrbitIncompatibleOwnedAugment()
    {
        for (int i = 0; i < currentSlotCount; i++)
        {
            AugmentSlotData slot = ownedSlots[i];
            if (slot == null || !slot.isOccupied || slot.augmentData == null) continue;

            if (IsOrbitIncompatibleAugment(slot.augmentData))
                return true;
        }

        return false;
    }

    private bool IsOrbitShotAugment(AugmentationSystem aug)
    {
        if (aug == null) return false;
        if (aug.category != AugmentationSystem.AugmentCategory.SubSkill) return false;
        if (aug.subSkillType != AugmentationSystem.SubSkillType.Shot) return false;

        return aug.shotModifierType == AugmentationSystem.ShotModifierType.Orbit;
    }

    /// <summary>
    /// 주위탄과 같이 보유하면 안 되는 증강인지 판단한다.
    /// 
    /// 금지 대상:
    /// 1. 차지샷
    /// 2. 반사
    /// 3. 관통
    /// 4. 연쇄
    /// 5. 패시브 자동연사
    /// 
    /// 허용 대상:
    /// 1. 3연발
    /// 2. 유도
    /// 3. 폭발
    /// 4. 화상
    /// 5. 일반 데미지 / 체력 / 흡혈 패시브
    /// </summary>
    private bool IsOrbitIncompatibleAugment(AugmentationSystem aug)
    {
        if (aug == null) return false;

        // 패시브는 이제 자동연사 포함해서 주위탄과 가능
        if (aug.category == AugmentationSystem.AugmentCategory.Passive)
        {
            return false;
        }

        if (aug.category != AugmentationSystem.AugmentCategory.SubSkill)
            return false;

        AugmentationSystem.SubSkillLevelData data = aug.GetSubSkillLevelData(1);
        if (data == null) return false;

        switch (aug.subSkillType)
        {
            case AugmentationSystem.SubSkillType.Shot:
                // 주위탄 + 차지샷만 금지
                return aug.shotModifierType == AugmentationSystem.ShotModifierType.Charge;

            case AugmentationSystem.SubSkillType.Trajectory:
                // 반사 / 관통 금지
                return data.bounceCount > 0 || data.pierceCount > 0;

            case AugmentationSystem.SubSkillType.Effect:
                // 연쇄 금지
                return data.chainCount > 0;
        }

        return false;
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