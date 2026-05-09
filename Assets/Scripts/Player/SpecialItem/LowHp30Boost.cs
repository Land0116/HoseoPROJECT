using UnityEngine;

[CreateAssetMenu(menuName = "Item/LowHp30Boost")]
public class LowHp30Boost : ItemData
{
    private bool prevState;

    public override void OnUpdate(PlayerController player)
    {
        bool current = (player.Hp / player.MaxHp <= 0.3f);

        if (current != prevState)
        {
            prevState = current;
            player.lowHp30Active = current;

            player.MarkStatsDirty();
        }
    }

    public override void ApplyEffectStat(PlayerController player)
    {
        if (!player.lowHp30Active) return;

        player.itemDamage += 2;
        player.MoveSpeed += 1;
    }
}