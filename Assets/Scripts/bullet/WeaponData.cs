using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("무기 스탯")]
    public float damage = 1f;

    [Header("발사용 총알")]
    public GameObject projectilePrefab;
}
