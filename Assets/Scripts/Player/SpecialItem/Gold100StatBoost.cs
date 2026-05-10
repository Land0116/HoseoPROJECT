using UnityEngine;

[CreateAssetMenu(menuName = "Item/Gold100StatBoost")]
public class Gold100StatBoost : ItemData
{
    [SerializeField] private float settingGold = 1100;
    private bool prevState;

    public override void OnUpdate(PlayerController player)
    {
        if (player == null) return;

        bool current = player.Gold >= settingGold;

        if (current != prevState)
        {
            prevState = current;
            player.gold100Active = current;

            player.MarkStatsDirty();
        }
    }

    public override void ApplyEffectStat(PlayerController player)
    {
        if (player == null) return;

        if (!player.gold100Active) return;

        player.MoveSpeed += 2f;
        player.itemDamage += 4f;
        player.attackPerSecond += 1f;
    }
}