using UnityEngine;
[CreateAssetMenu(menuName = "Item/ThiefHat")]
public class ThiefHat : ItemData
{
    public override float GetGoldMultiplier()
    {
        return 1.2f;
    }
    public override float GetShopDiscount()
    {
        return 0.15f;
    }
}
