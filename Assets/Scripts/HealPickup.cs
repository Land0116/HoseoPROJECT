using UnityEngine;

public class HealPickup : MonoBehaviour, IShopInteractable
{
    [Header("회복량")]
    [SerializeField, Range(0f, 1f)] private float healPercent = 0.25f;

    private PlayerController player;
    private bool isPlayerInside = false;

    // =========================
    // 상호작용 (핵심)
    // =========================
    public void Interact(PlayerController player)
    {
        if (player == null) return;

        // [수정] UI 허용 상태 아닐 때 사용 불가
        if (!CombatRoomController.IsCombatInteractable) return;

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
        isPlayerInside = true;

        // [수정] UI 허용 상태일 때만 등록
        if (CombatRoomController.IsCombatInteractable)
        {
            ItemUIManager.Instance?.RegisterShop(this);
            ForceShowUI();
        }
    }

    // =========================
    // 범위 이탈
    // =========================
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        player = null;
        isPlayerInside = false;

        ItemUIManager.Instance?.UnregisterShop(this);
        ItemUIManager.Instance?.HideShopInteract();
    }

    public void TryShowUI()
    {
        // [수정] UI 허용 상태 아닐 때 차단
        if (!CombatRoomController.IsCombatInteractable) return;

        ShowUI();
    }

    private void ShowUI()
    {
        if (ItemUIManager.Instance == null) return;

        string title = "하급 포션";
        string category = "회복 아이템";

        int percent = Mathf.RoundToInt(healPercent * 100f);

        string effect = $"{percent}%";
        string desc = "체력을 조금 회복한다.";

        // [수정] UI 허용 상태일 때만 표시
        if (CombatRoomController.IsCombatInteractable)
        {
            ItemUIManager.Instance.ShowShopInteract(this, null);
            ItemUIManager.Instance.ShowPotionTooltip(title, category, effect, desc);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void ForceShowUI()
    {
        // [수정] 강제 UI도 동일하게 제한
        if (!CombatRoomController.IsCombatInteractable) return;

        ShowUI();
    }
}