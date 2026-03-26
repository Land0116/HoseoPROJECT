using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{

    [Header("æ∆¿Ã≈€ Ω∫≈»")]
    public float damage = 1f;
    public float moveSpeed = 0f;
    public float bulletRate = 0f;
    public int hp = 0;


    public Sprite icon;
    public string itemName;
}