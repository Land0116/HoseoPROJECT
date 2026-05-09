using UnityEngine;

[CreateAssetMenu(menuName = "Item/CurseTalisman")]
public class CurseTalisman : ItemData
{

    public override float GetGoldMultiplier()
    {
        return 1.5f; 
    }
}