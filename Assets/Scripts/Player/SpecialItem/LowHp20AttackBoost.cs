using UnityEngine;

[CreateAssetMenu(menuName = "Item/LowHp20AttackBoost")]
public class LowHp20AttackBoost : ItemData
{
    private bool prevState;

    public override void OnUpdate(PlayerController player)
    {
        if (player == null) return;

        bool current = player.Hp <= 20f;

        if (current != prevState)
        {
            prevState = current;
            player.lowHp20Active = current;

            player.MarkStatsDirty();
        }
    }

    public override void ApplyEffectStat(PlayerController player)
    {
        if (player == null) return;

        // 기본 효과 (항상 적용됨)
        //player.maxHp += hp;

        // 조건 효과
        if (!player.lowHp20Active) return;

        player.attackPerSecond += 5f;
        player.itemDamage += 2f;
    }
}