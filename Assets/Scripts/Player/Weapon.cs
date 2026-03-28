using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    // [Header("무기스텟")]
    // public float damage = 1f;
    //
    // [Header("발사용 총알")]
    // public GameObject projectilePrefab;

    private bool isPlayerNear = false;

    private void Update()
    {
        if(isPlayerNear && Keyboard.current.fKey.wasPressedThisFrame)
        {
            
            Debug.Log("F키로 상호작용 - 총알");
            //PlayerController.Instance.EquipWeapon();
            //Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
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
