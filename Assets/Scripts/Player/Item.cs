using UnityEngine;
using UnityEngine.InputSystem;

public class Item : MonoBehaviour
{
    [Header("æ∆¿Ã≈€ Ω∫≈›")]
    public int itemStage;
    public float damage = 1f;
    public float moveSpeed = 0f;
    public float bulletRate = 0f;
    public int hp = 0;

    private bool isPlayerNear = false;

    private void Update()
    {
        if(isPlayerNear && Keyboard.current.fKey.wasPressedThisFrame)
        {
           
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;

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
