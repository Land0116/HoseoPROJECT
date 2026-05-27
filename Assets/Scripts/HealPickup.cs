using UnityEngine;

public class HealPickup : MonoBehaviour, IShopInteractable
{
    [Header("회복량")]
    [SerializeField, Range(0f, 1f)] private float healPercent = 0.25f;

    private PlayerController player;

    // =========================
    // 상호작용 (핵심)
    // =========================
    public void Interact(PlayerController player)
    {
        if (player == null) return;

        float healAmount = player.MaxHp * healPercent;
        player.Heal(healAmount);

        Destroy(gameObject);
    }

    // =========================
    // 범위 진입
    // =========================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        player = collision.GetComponent<PlayerController>();

        ItemUIManager.Instance?.RegisterShop(this);
        ShowUI();
    }

    // =========================
    // 범위 이탈
    // =========================
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        player = null;

        ItemUIManager.Instance?.UnregisterShop(this);
        ItemUIManager.Instance?.HideShopInteract();
    }

    private void ShowUI()
    {
        if (ItemUIManager.Instance == null) return;

        ItemUIManager.Instance.ShowShopInteract(
            this,
            "[체력 회복 아이템]\n[F]키로 체력 25% 회복"
        );
    }

    public Transform GetTransform()
    {
        return transform;
    }
}