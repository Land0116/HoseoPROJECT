using UnityEngine;
using UnityEngine.UI;

public class CurWeaponUI : MonoBehaviour
{
    public static CurWeaponUI Instance;

    [SerializeField] private Image weaponImage;

    [Header("게임 시작 시 장착된 기본 무기")]
    [SerializeField] private WeaponData defaultWeapon;

    private void Awake()
    {
        Instance = this;

        if (defaultWeapon != null && defaultWeapon.icon != null)
        {
            weaponImage.sprite = defaultWeapon.icon;
            weaponImage.enabled = true;
        }
        else
        {
            weaponImage.enabled = false;
        }
    }

    public void SetWeapon(WeaponData weaponData)
    {
        if (weaponData != null && weaponData.icon != null)
        {
            weaponImage.sprite = weaponData.icon;
            weaponImage.enabled = true;
        }
        else
        {
            weaponImage.enabled = false;
        }
    }
}