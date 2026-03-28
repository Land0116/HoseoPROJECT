using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    [Header("줍게 될 무기 데이터")]
    [SerializeField] private WeaponData weaponData;

    private bool isPlayerNear = false;

    private void Update()
    {
        if (isPlayerNear && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("F키로 상호작용 - 무기 획득");

            PlayerController.Instance.EquipWeapon(weaponData);

            // 픽업 오브젝트만 삭제
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}
