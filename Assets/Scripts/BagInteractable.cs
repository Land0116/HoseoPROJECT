using UnityEngine;
using UnityEngine.InputSystem;

public class BagInteractable : MonoBehaviour, IInteractable
{
    private bool isPlayerInRange = false;
    private PlayerController player;
    private NewItemUIManager shopUIManager;
    private static BagInteractable currentTarget;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        shopUIManager = FindFirstObjectByType<NewItemUIManager>();
    }
    private void Update()
    {
        if (!isPlayerInRange) return;

        if (currentTarget != this) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("F ´­¸²");
            Interact(player);
        }
    }

    public void Interact(PlayerController player)
    {
        if (shopUIManager != null)
        {
            shopUIManager.IsOpenedItem = true; 
        }

        if (ItemUIManager.Instance != null)
        {
            ItemUIManager.Instance.HideInteractPanel(); //*
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.GetComponent<PlayerController>();
        isPlayerInRange = true;

        currentTarget = this;

        ItemUIManager.Instance?.RegisterBag(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = null;
        isPlayerInRange = false;

        ItemUIManager.Instance?.UnregisterBag(this);
    }
}
