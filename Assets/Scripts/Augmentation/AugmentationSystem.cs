using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AugmentationSystem", menuName = "Augmentation/AugmentationSystem")]
public class AugmentationSystem : ScriptableObject
{
    #region Enum

    public enum AugmentCategory
    {
        SubSkill, //서브스킬형
        Passive, //패시브형
        Special //스페셜형
    }

    public enum SubSkillType
    {
        None,
        Shot,
        Trajectory,
        Effect
    }

    public enum PassiveType
    {
        None,
        Damage,
        AttackSpeed,
        MaxHp,
        LifeSteal,
    }

    public enum SpecialType
    {
        None,
        MoveSpeedBoost, // 이동 속도 증가
        CharacterShrink, // 캐릭터 축소
        IgnoreObstacleCollision, // 장애물 충돌 무시
        CheatDeath, // 치명적 피해 극복
        HpToDamageByMaxHp, // 체력 -> 공격력 변환
        HealOnKill, // 적 처치 시 체력 회복
        HpRegen, // 초당 체력 회복
        HealToAutoAttack, // 회복 시 자동 공격
        PeriodicShield // 주기적 보호막
    }

    public enum StackRule
    {
        None,
        LevelUpSameSkill,
        StackSameEffect,
        Unique
    }

    public enum ShotFireMode
    {
        Single, // 기본 단발 패턴
        MultiShot, // 산탄처럼 동시에 여러 발
        Burst // 3연발처럼 시간차 연사
    }

    public enum ShotModifierType
    {
        None,
        Burst, // 3연발
        MultiShot, // 산탄
        Charge, // 차지샷
        Orbit // 주위탄
    }

    [Header("발사형 세부 타입")] public ShotModifierType shotModifierType = ShotModifierType.None;

    #endregion

    #region Level Data

    [Serializable]
    public class SubSkillLevelData
    {
        [Min(1)] public int level = 1;
        [Header("레벨별 설명")] [TextArea] public string levelDescription;

        [Header("Tooltip")] [TextArea] public string tooltipEffectText;
        [TextArea] public string tooltipDescription;

        [Header("발사")] public ShotFireMode shotFireMode = ShotFireMode.Single;
        public float burstInterval = 0f;
        public int projectileCountAdd = 0;
        public float damageMultiplier = 1f;
        public float spreadAngle = 0f;
        public float chargeTime = 0f;

        [Tooltip("자동연사처럼 공격 속도를 올리는 경우 사용. 0.20 = 20% 증가")]
        public float attackSpeedBonusPercent = 0f;

        [Tooltip("산탄처럼 유효 사거리를 줄일 때 사용. 1 = 기본, 0.6 = 40% 감소")]
        public float projectileLifeMultiplier = 1f;

        [Header("궤적")] public float homingStrength = 0f;
        public float trajectoryDuration = 0f;
        public int bounceCount = 0;
        public int pierceCount = 0;

        [Header("주위탄")] public bool useOrbitProjectile = false; // 이 레벨에서 주위탄을 사용하는지
        public int orbitProjectileCount = 0; // 주위탄 개수
        public float orbitRadius = 1.5f; // 플레이어로부터 도는 반경
        public float orbitAngularSpeed = 180f; // 초당 회전 각도
        public float orbitHitInterval = 0.2f; // 같은 적을 다시 때릴 수 있는 최소 간격
        public float orbitDamageMultiplier = 1f; // 주위탄 전용 데미지 배율
        public float orbitLifetime = 0f; // 0이면 무한 유지

        [Header("효과")] public float explosionRadius = 0f;
        public float explosionDamageMultiplier = 1f;
        public int chainCount = 0;
        public float chainRange = 0f;
        public float dotDamagePerSecond = 0f;
        public float dotDuration = 0f;
    }

    [Serializable]
    public class PassiveLevelData
    {
        [Min(1)] public int level = 1;
        [Header("레벨별 설명")] [TextArea] public string levelDescription;

        [Header("Tooltip")] [TextArea] public string tooltipEffectText;
        [TextArea] public string tooltipDescription;

        [Header("기본 패시브 수치")] public float damageMultiplier = 1f;
        public float attackSpeedPercent = 0f;
        public int maxHpAdd = 0;
        public float lifeStealPercent = 0f;
    }

    [Serializable]
    public class SpecialLevelData
    {
        [Min(1)] public int level = 1;

        [Header("이동 / 외형 / 충돌")] public float moveSpeedMultiplier = 1f;
        public float moveSpeedAdd = 0f;
        public float characterScaleMultiplier = 1f;
        public bool ignoreObstacleCollision = false;

        [Header("생존")] public bool enableCheatDeath = false;
        public int cheatDeathRemainHp = 1;
        public float cheatDeathInvincibleDuration = 1f;

        [Header("공격 / 회복")] [Tooltip("최대 체력 10당 최종 데미지 추가 배율. 5%면 0.05")]
        public float hpToDamagePercentPer10Hp = 0f;

        public float healOnKill = 0f;
        public float hpRegenPerSecond = 0f;

        [Header("회복 시 자동 공격")] public float healToAutoAttackThreshold = 0f;
        public float healToAutoAttackDamageMultiplier = 0f;

        [Header("보호막")] public float shieldInterval = 0f;
        public int shieldMaxCount = 0;

        [TextArea] public string ruleDescription;

        [Header("Tooltip")] [TextArea] public string tooltipEffectText;
        [TextArea] public string tooltipDescription;
    }

    #endregion

    #region Inspector Fields

    [Header("기본 정보")] public string augmentID;
    public string augmentationName;
    [TextArea] public string augmentationDesc;
    [Header("UI 이미지")] public Sprite icon;
    public Sprite slotSprite;

    [Header("분류")] public AugmentCategory category;
    public SubSkillType subSkillType = SubSkillType.None;
    public PassiveType passiveType = PassiveType.None;
    public SpecialType specialType = SpecialType.None;
    public StackRule stackRule = StackRule.None;

    [Header("등장 설정")] public bool isUnlocked = true;
    [Min(0)] public int weight = 100;

    [Header("성장 설정")] [Min(1)] public int maxLevel = 1;

    [Header("레벨 데이터")] public SubSkillLevelData[] subSkillLevels;
    public PassiveLevelData[] passiveLevels;
    public SpecialLevelData[] specialLevels;

    [Header("상점 가격")] [Min(0)] public int goldCost = 0;

    #endregion

    #region Get Level Data

    public SubSkillLevelData GetSubSkillLevelData(int level)
    {
        if (subSkillLevels == null || subSkillLevels.Length == 0)
            return null;

        int targetLevel = Mathf.Clamp(level, 1, maxLevel);

        for (int i = 0; i < subSkillLevels.Length; i++)
        {
            if (subSkillLevels[i] != null && subSkillLevels[i].level == targetLevel)
                return subSkillLevels[i];
        }

        return null;
    }

    public PassiveLevelData GetPassiveLevelData(int level)
    {
        if (passiveLevels == null || passiveLevels.Length == 0)
            return null;

        int targetLevel = Mathf.Clamp(level, 1, maxLevel);

        for (int i = 0; i < passiveLevels.Length; i++)
        {
            if (passiveLevels[i] != null && passiveLevels[i].level == targetLevel)
                return passiveLevels[i];
        }

        return null;
    }

    public SpecialLevelData GetSpecialLevelData(int level)
    {
        if (specialLevels == null || specialLevels.Length == 0)
            return null;

        int targetLevel = Mathf.Clamp(level, 1, maxLevel);

        for (int i = 0; i < specialLevels.Length; i++)
        {
            if (specialLevels[i] != null && specialLevels[i].level == targetLevel)
                return specialLevels[i];
        }

        return null;
    }

    #region Tooltip

    public string GetTooltipCategoryText()
    {
        switch (category)
        {
            case AugmentCategory.SubSkill:
                return "서브 스킬형 증강";

            case AugmentCategory.Passive:
                return "패시브형 증강";

            case AugmentCategory.Special:
                return "스페셜형 증강";

            default:
                return "분류 없음";
        }
        
    }

    public string GetTooltipEffectText(int level)
    {
        int targetLevel = Mathf.Clamp(level, 1, maxLevel);

        switch (category)
        {
            case AugmentCategory.SubSkill:
            {
                SubSkillLevelData data = GetSubSkillLevelData(targetLevel);
                if (data == null) return string.Empty;

                return string.IsNullOrWhiteSpace(data.tooltipEffectText)
                    ? data.levelDescription
                    : data.tooltipEffectText;
            }

            case AugmentCategory.Passive:
            {
                PassiveLevelData data = GetPassiveLevelData(targetLevel);
                if (data == null) return string.Empty;

                return string.IsNullOrWhiteSpace(data.tooltipEffectText)
                    ? data.levelDescription
                    : data.tooltipEffectText;
            }

            case AugmentCategory.Special:
            {
                SpecialLevelData data = GetSpecialLevelData(targetLevel);
                if (data == null) return string.Empty;

                return string.IsNullOrWhiteSpace(data.tooltipEffectText)
                    ? data.ruleDescription
                    : data.tooltipEffectText;
            }
        }

        return string.Empty;
    }

    public string GetTooltipDescription(int level)
    {
        int targetLevel = Mathf.Clamp(level, 1, maxLevel);

        switch (category)
        {
            case AugmentCategory.SubSkill:
            {
                SubSkillLevelData data = GetSubSkillLevelData(targetLevel);
                if (data == null) return augmentationDesc;

                return string.IsNullOrWhiteSpace(data.tooltipDescription)
                    ? augmentationDesc
                    : data.tooltipDescription;
            }

            case AugmentCategory.Passive:
            {
                PassiveLevelData data = GetPassiveLevelData(targetLevel);
                if (data == null) return augmentationDesc;

                return string.IsNullOrWhiteSpace(data.tooltipDescription)
                    ? augmentationDesc
                    : data.tooltipDescription;
            }

            case AugmentCategory.Special:
            {
                SpecialLevelData data = GetSpecialLevelData(targetLevel);
                if (data == null) return augmentationDesc;

                return string.IsNullOrWhiteSpace(data.tooltipDescription)
                    ? augmentationDesc
                    : data.tooltipDescription;
            }
        }

        return augmentationDesc;
    }

    #endregion

    #endregion
}