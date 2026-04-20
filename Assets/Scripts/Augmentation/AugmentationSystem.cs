using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AugmentationSystem", menuName = "Augmentation/AugmentationSystem")]
public class AugmentationSystem : ScriptableObject
{
    #region Enum

    public enum AugmentCategory
    {
        SubSkill,   // 서브 스킬형
        Passive,    // 패시브형
        Special     // 특수형
    }

    public enum SubSkillType
    {
        None,
        Shot,       // 발사
        Trajectory, // 궤적
        Effect      // 효과
    }

    public enum PassiveType
    {
        None,
        Damage,         // 공격력 증가
        AttackSpeed,    // 공격 속도 증가
        MaxHp,          // 최대 체력 증가
        LifeSteal       // 흡혈
    }

    public enum SpecialType
    {
        None,
        UniqueRule      // 현재 문서상 구체 효과 미정, 고유 규칙형으로만 분류
    }

    public enum StackRule
    {
        None,
        LevelUpSameSkill,   // 동일 스킬이면 레벨 증가
        StackSameEffect,    // 동일 효과면 중첩
        Unique              // 중복 불가
    }
    public enum ShotFireMode
    {
        Single,     // 기본 단발
        MultiShot,  // 산탄처럼 동시에 여러 발
        Burst       // 3연발처럼 시간차 연사
    }
    #endregion

    #region Level Data
 
    [Serializable]
    public class SubSkillLevelData
    {
        [Min(1)] public int level = 1;

        [Header("발사")]
        public ShotFireMode shotFireMode = ShotFireMode.Single;
        public float burstInterval = 0f;
        public int projectileCountAdd = 0;      // 기본 1발에서 추가 수
        public float damageMultiplier = 1f;     // 발사 구조 변경으로 인한 데미지 배율
        public float spreadAngle = 0f;          // 산탄 등 부채꼴 각도
        public float chargeTime = 0f;           // 차지샷용

        [Header("궤적")]
        public float homingStrength = 0f;
        public float trajectoryDuration = 0f;
        public int bounceCount = 0;
        public int pierceCount = 0;

        [Header("효과")]
        public float explosionRadius = 0f;
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

        public float damageMultiplier = 1f;     // 1.15 = 15% 증가
        public float attackSpeedPercent = 0f;   // 0.20 = 20% 증가
        public int maxHpAdd = 0;
        public float lifeStealPercent = 0f;     // 0.05 = 5%
    }

    [Serializable]
    public class SpecialLevelData
    {
        [Min(1)] public int level = 1;
        [TextArea] public string ruleDescription;
    }

    #endregion

    #region Inspector Fields

    [Header("기본 정보")]
    public string augmentID;
    public string augmentationName;
    [TextArea] public string augmentationDesc;
    public Sprite icon;

    [Header("분류")]
    public AugmentCategory category;
    public SubSkillType subSkillType = SubSkillType.None;
    public PassiveType passiveType = PassiveType.None;
    public SpecialType specialType = SpecialType.None;
    public StackRule stackRule = StackRule.None;

    [Header("등장 설정")]
    public bool isUnlocked = true;
    [Min(0)] public int weight = 100;

    [Header("성장 설정")]
    [Min(1)] public int maxLevel = 1;

    [Header("레벨 데이터")]
    public SubSkillLevelData[] subSkillLevels;
    public PassiveLevelData[] passiveLevels;
    public SpecialLevelData[] specialLevels;

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

    #endregion
}