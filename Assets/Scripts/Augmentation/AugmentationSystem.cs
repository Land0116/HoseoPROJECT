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
    
    [Header("증강시스템정보")]
    public AugmentationType augmentationType;
    public string augmentationName;
    [TextArea]
    public string augmentationDesc;

    [Header("증강시스템레벨")]
    public AugmentEffectType effectType;
    public float value;
    public float duration;
    public Sprite icon;

}
