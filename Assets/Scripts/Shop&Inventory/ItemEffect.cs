using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public virtual bool HasCondition => false;
    public virtual void OnUpdate(PlayerController player)
    {
        // 기본은 아무것도 안함
    }
    public virtual void ApplyStat(PlayerController player)
    {
        // 기본은 아무것도 안함
    }
    public virtual bool ShouldApply(PlayerController player)
    {
        return true;
    }

    public virtual float GetGoldMultiplier()
    {
        return 1f;
    }

    public virtual float GetShopDiscount()
    {
        return 0f;
    }
}