using UnityEngine;

[CreateAssetMenu(fileName = "Item_CooldownReduce", menuName = "Item/CooldownReduce")]
public class Item_CooldownReduce : ItemData
{
    [SerializeField] private float reduceAmount = 1f;

    public override void ApplyEffectStat(PlayerController player)
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.AddCooldownReduction(reduceAmount);
        }
    }
}