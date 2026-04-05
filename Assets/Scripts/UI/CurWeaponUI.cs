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
        //Debug.Log("CurWeaponUI Awake 실행됨");
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (defaultWeapon != null && defaultWeapon.icon != null)
        {
            //Debug.Log("기본 무기 아이콘 세팅");
            weaponImage.sprite = defaultWeapon.icon;
            weaponImage.enabled = true;
        }
        else
        {
            //Debug.Log("기본 무기 없음 or 아이콘 없음");
            weaponImage.enabled = false;
        }
    }
    

    public void SetWeapon(WeaponData weaponData)
    {
        //Debug.Log("SetWeapon 호출됨: " + weaponData);
        if (weaponData != null && weaponData.icon != null)
        {
            //Debug.Log("아이콘 적용됨");
            weaponImage.sprite = weaponData.icon;
            weaponImage.enabled = true;
        }
        else
        {
            //Debug.Log("아이콘 없음");
            weaponImage.enabled = false;
        }
    }



}