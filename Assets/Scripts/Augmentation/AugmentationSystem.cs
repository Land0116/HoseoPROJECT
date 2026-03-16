using UnityEngine;

[CreateAssetMenu(fileName = "AugmentationSystem", menuName = "Augmentation/AugmentationSystem")]
public class AugmentationSystem : ScriptableObject
{
    public enum AugmentationType { Atk, Dfs, Util }

    public enum AugmentEffectType
    {
        BulletDamage,       // 총알 공격력 증가
        ItemDamage,         // 아이템 공격력 증가
        BulletCount,        // 총알 발사 수 증가
        AttackSpeed,        // 공격속도 증가

        Shield,             // 보호막
        MaxHP,              // 최대체력 증가
        HPRegen,            // 체력회복 증가

        CharacterScale,     // 캐릭터 크기 변경
        IgnoreObstacle,     // 장애물 충돌 무시
        RemoveSightBlock,   // 시야 방해 제거
        MoveSpeed,          // 이동 속도 증가
        LifeSteal           // 흡혈
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
