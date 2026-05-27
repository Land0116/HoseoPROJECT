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

        string title = "하급 포션";
        string category = "회복 아이템";

        // 실제 healPercent 기반으로 표시 (유연하게)
        int percent = Mathf.RoundToInt(healPercent * 100f);

        string effect = $"{percent}%";
        string desc = "체력을 조금 회복한다.";

        // 위치 UI 활성화
        ItemUIManager.Instance.ShowShopInteract(this, null);

        // 포션 툴팁 표시
        ItemUIManager.Instance.ShowPotionTooltip(title, category, effect, desc);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}