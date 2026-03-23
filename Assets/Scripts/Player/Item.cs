using UnityEngine;
using UnityEngine.InputSystem;

public class Item : MonoBehaviour
{
    [Header("아이템 스텟")]
    public float damage = 1f;
    public float moveSpeed = 0f;
    public float bulletRate = 0f;
    public int hp = 0;

    private bool isPlayerNear = false;

    private void Update()//아이템 교환 기존아이템 사라짐
    {
        if(isPlayerNear && Keyboard.current.fKey.wasPressedThisFrame)
        {
            PlayerController.Instance.EquipItem(this);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;

            //나중에 UI띄우기
            Debug.Log("F키로 상호작용 가능");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}
