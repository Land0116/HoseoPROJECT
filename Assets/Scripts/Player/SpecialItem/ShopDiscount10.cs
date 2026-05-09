using UnityEngine;

[CreateAssetMenu(menuName = "Item/ShopDiscount10")]
public class ShopDiscount10 : ItemData
{
    public override float GetShopDiscount()
    {
        return 0.1f;
    }
}