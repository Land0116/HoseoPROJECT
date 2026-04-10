using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int price;

    [TextArea]
    public string description;

    [Header("아이템 능력")]
    public float cooldown;

    public ItemEffect effect;
}