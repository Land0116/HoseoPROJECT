using UnityEngine;

[CreateAssetMenu(fileName = "EquipScriptableObject", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("무기 이름")]
    public string weaponName;

    [Header("무기 스탯")]
    public float damage = 1f;

    public Sprite icon;
    [Header("발사용 총알")]
    public GameObject projectilePrefab;
}