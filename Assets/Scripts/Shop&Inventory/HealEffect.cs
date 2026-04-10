using UnityEngine;

[CreateAssetMenu(menuName = "ItemEffect/Heal")]
public class HealEffect : ItemEffect
{
    public int healAmount;

    public override void Use(GameObject user)
    {
        PlayerController player = user.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Heal(healAmount);
        }
    }
}