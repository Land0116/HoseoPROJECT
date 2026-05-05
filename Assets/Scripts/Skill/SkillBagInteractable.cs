using UnityEngine;
using UnityEngine.InputSystem;

public class SkillBagInteractable : MonoBehaviour, IInteractable
{
    private bool isPlayerInRange = false;
    private PlayerController player;
    private static SkillBagInteractable currentTarget;

    private void Update()
    {
        if (!isPlayerInRange) return;
        if (currentTarget != this) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Interact(player);
        }
    }

    public void Interact(PlayerController player)
    {


        if (SkillSelectUIManager.Instance != null)
        {
            SkillSelectUIManager.Instance.IsSkillOpened = true;
        }



        ItemUIManager.Instance?.HideInteractPanel();

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.GetComponent<PlayerController>();
        isPlayerInRange = true;

        currentTarget = this;

        ItemUIManager.Instance?.RegisterBag(null);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = null;
        isPlayerInRange = false;
    }
}