using UnityEngine;

[CreateAssetMenu(fileName = "AttackSpeedPlusItem", menuName = "Item/Passive/AttackSpeedPlusItem")]
public class AttackSpeedPlusItem : ItemData
{
    private bool prevState;

    public override void OnUpdate(PlayerController player)
    {
        if (player == null) return;

        bool current = player.AttackPerSecond >= 6f;

        // 상태 변화 감지
        if (current != prevState)
        {
            prevState = current;

            player.attackSpeed6Active = current;
            player.MarkStatsDirty();
        }
    }

    public override void ApplyEffectStat(PlayerController player)
    {
        if (player == null) return;

        // 조건이 아니면 아무 효과 없음
        if (!player.attackSpeed6Active) return;

        player.MoveSpeed += 1.5f;
    }
}