using UnityEngine;

[CreateAssetMenu(menuName = "Item/DisplayTicket")]
public class DisplayTicket : ItemData
{
    [Header("특수 아이템 확률 추가 (퍼센트 포인트)")]
    public float specialChanceAdd = 20f; // +20%

    public override void ApplyEffectStat(PlayerController player)
    {
        if (player == null) return;

        player.hasDisplayTicket = true;
    }
}