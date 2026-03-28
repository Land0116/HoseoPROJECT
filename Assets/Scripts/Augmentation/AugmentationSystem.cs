using UnityEngine;

[CreateAssetMenu(fileName = "AugmentationSystem", menuName = "Augmentation/AugmentationSystem")]
public class AugmentationSystem : ScriptableObject
{
    public enum AugmentationType { Atk, Dfs, Util }

    public enum AugmentEffectType
    {
        // =========================
        // [최종 데미지 관련]
        // =========================
        DamageMultiplier,            // 총알/아이템 데미지 증가
        SlowDamageMultiplier,        // 느리지만 강한 공격
        
        // =========================
        // [공격 관련]
        // =========================
        BulletCountAdd,              // 한 번 발사 시 총알 수 +N
        AttackPerSecondAdd,          // 초당 공격 횟수 증가/감소
        
        // =========================
        // [체력 / 방어 관련]
        // =========================
        MaxHpMultiplier,             // 최대 체력 x계수
        HpToDamagePercentPerHp,      // 최대 체력 10당 최종 데미지 % 증가
        HealOnKill,                  // 적 처치 시 체력 회복
        HpRegenPerSecond,            // 초당 체력 회복
        HealToAutoAttack,            // 누적 회복량이 기준치 도달 시 자동 공격 발동
        LifeStealOnHit,              // 공격 적중 시 회복
        ShieldByInterval,            // N초마다 1회 공격 무효
        
        // =========================
        // [이동 / 유틸 관련]
        // =========================
        MoveSpeedMultiplier,         // 이동속도 x계수
        CharacterScaleMultiplier,    // 캐릭터 크기 x계수
        IgnoreObstacleCollision,     // 장애물 충돌 무시
        CheatDeathOnce,              // 1회 치명상 무시
        //InvincibleSeconds          // N초 무적
    }
    
    [Header("증강 기본 정보")]
    public string augmentationID;
    public AugmentationType augmentationType;
    public string augmentationName;
    [TextArea]
    public string augmentationDesc;
    public Sprite icon;

    [Header("증강 효과 정보")]
    public AugmentEffectType effectType;
    public float value;        // 효과 수치
    public float duration;     // 지속/주기 시간 등에 사용

    [Header("증강 등장 설정")]
    public int maxStack = 1;   // 현재 시스템은 중복 불가라 기본 1
    public bool isUnlocked = true;
}
