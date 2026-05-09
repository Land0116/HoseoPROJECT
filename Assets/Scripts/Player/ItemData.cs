using UnityEngine;
public enum ItemZoneType
{
    TutoZone,
    StorageZone,
    SortZone,
    NormalZone,
    PremiumZone
}
[CreateAssetMenu(fileName = "ItemData", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("아이템 스탯")]
    public int stage;

    public float damage = 0f;
    public float moveSpeed = 0f;
    public float bulletRate = 0f;
    public int hp = 0;
    public int itemPrice = 0;

    public Sprite icon;
    public string itemName;
    public ItemZoneType zoneType;
    [Header("설명")]
    [TextArea]
    public string description;

    public virtual void OnUpdate(PlayerController player) { }

    public virtual bool ShouldApply(PlayerController player)
    {
        return true;
    }

    public virtual void ApplyEffectStat(PlayerController player) { }

    public virtual float GetGoldMultiplier()
    {
        return 1f;
    }

    public virtual float GetShopDiscount()
    {
        return 0f;
    }
}