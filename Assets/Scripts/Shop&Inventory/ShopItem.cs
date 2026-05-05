using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopItem : MonoBehaviour, IInteractable, IShopInteractable
{
    [SerializeField] private GameObject worldItemPrefab;

    [Header("===== Stage 1 =====")]
    public ItemData[] stage1NormalItems;
    public ItemData[] stage1PremiumItems;

    [Header("===== Stage 2 =====")]
    public ItemData[] stage2NormalItems;
    public ItemData[] stage2PremiumItems;

    [Header("===== Stage 3 =====")]
    public ItemData[] stage3NormalItems;
    public ItemData[] stage3PremiumItems;

    [Range(1, 3)]
    public int currentStage = 1;

    [Header("포션 아이콘")]
    [SerializeField] private Sprite potion25Icon;
    [SerializeField] private Sprite potion50Icon;
    [SerializeField] private Sprite potion75Icon;

    [Header("UI")]
    public Image iconImage;
    public TMP_Text priceText;

    private ItemData currentItem;

    private bool isPotion;
    private float potionHealPercent;

    private int price;

    private bool isPlayerInRange;
    private PlayerController player;

    private void Start()
    {
        GenerateItem();
    }

    private void Update()
    {
        if (!isPlayerInRange) return;
        if (player == null) return;

    }

    // =========================
    // Interact (구매 핵심)
    // =========================
    public void Interact(PlayerController player)
    {

        if (player == null) return;

        if (player.Gold < price)
        {

            return;
        }

        if (!isPotion && currentItem == null)
        {

            return;
        }

        player.Gold -= price;

        if (isPotion)
        {
            ApplyPotion(player);
        }
        else
        {
            ApplyItem(player);
        }

        gameObject.SetActive(false);
    }

    // =========================
    // 아이템 적용 (핵심 구조)
    // =========================
    private void ApplyItem(PlayerController player)
    {
        ItemData itemToGive = currentItem;

        bool success = player.EquipItem(itemToGive);

        if (!success)
        {
            Debug.Log("[ShopItem] 인벤토리 가득참 → 아이템 월드 생성");

            SpawnWorldItem(itemToGive);
        }
        else
        {
            CurItemUI.Instance?.SetItems(player.GetEquippedItems());
        }
    }

    // =========================
    // 월드 드랍
    // =========================
    private void SpawnWorldItem(ItemData item)
    {
        if (worldItemPrefab == null)
        {
            Debug.LogError("worldItemPrefab 없음");
            return;
        }

        GameObject obj = Instantiate(worldItemPrefab, transform.position, Quaternion.identity);

        ItemPickup pickup = obj.GetComponent<ItemPickup>();

        if (pickup == null)
        {
            Debug.LogError("ItemPickup 없음 (프리팹 확인)");
            return;
        }

        pickup.SetItemData(item);

        Debug.Log($"[ShopItem] 아이템 드랍 생성 = {item.itemName}");
    }

    // =========================
    // 포션
    // =========================
    private void ApplyPotion(PlayerController player)
    {
        float healAmount = player.MaxHp * potionHealPercent;
        player.Heal(healAmount);
    }

    // =========================
    // 아이템 생성
    // =========================
    private void GenerateItem()
    {
        ItemData[] normalPool = null;
        ItemData[] premiumPool = null;

        switch (currentStage)
        {
            case 1:
                normalPool = stage1NormalItems;
                premiumPool = stage1PremiumItems;
                break;
            case 2:
                normalPool = stage2NormalItems;
                premiumPool = stage2PremiumItems;
                break;
            case 3:
                normalPool = stage3NormalItems;
                premiumPool = stage3PremiumItems;
                break;
        }

        float rand = Random.value;

        if (rand <= 0.7f && normalPool != null && normalPool.Length > 0)
        {
            currentItem = normalPool[Random.Range(0, normalPool.Length)];
            isPotion = false;
            price = Random.Range(75, 86);
        }
        else if (rand <= 0.8f && premiumPool != null && premiumPool.Length > 0)
        {
            currentItem = premiumPool[Random.Range(0, premiumPool.Length)];
            isPotion = false;
            price = Random.Range(140, 161);
        }
        else
        {
            isPotion = true;
            currentItem = null;

            int potionType = Random.Range(0, 3);

            if (potionType == 0) { potionHealPercent = 0.25f; price = 25; }
            else if (potionType == 1) { potionHealPercent = 0.5f; price = 50; }
            else { potionHealPercent = 0.75f; price = 100; }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (isPotion)
        {
            if (potionHealPercent == 0.25f) iconImage.sprite = potion25Icon;
            else if (potionHealPercent == 0.5f) iconImage.sprite = potion50Icon;
            else iconImage.sprite = potion75Icon;
        }
        else if (currentItem != null)
        {
            iconImage.sprite = currentItem.icon;
        }

        priceText.text = price.ToString();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = true;
        player = collision.GetComponent<PlayerController>();

        ItemUIManager.Instance?.RegisterShop(this);

        ShowUI();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = false;
        player = null;
        ItemUIManager.Instance?.UnregisterShop(this);
        ItemUIManager.Instance?.HideShopInteract();
    }
    private void ShowUI()
    {
        if (isPotion)
        {
            string name = "";
            string desc = "";

            if (potionHealPercent == 0.25f)
            {
                name = "하급 체력 회복 포션";
                desc = "현재 체력을 25% 회복한다.";
            }
            else if (potionHealPercent == 0.5f)
            {
                name = "중급 체력 회복 포션";
                desc = "현재 체력을 50% 회복한다.";
            }
            else if (potionHealPercent == 0.75f)
            {
                name = "상급 체력 회복 포션";
                desc = "현재 체력을 75% 회복한다.";
            }

            ItemUIManager.Instance.ShowShopInteract(
                this,
                $"[{name}]\n{desc}"
            );

            return;
        }

        // 일반 아이템일 때
        if (currentItem == null) return;

        ItemUIManager.Instance.ShowShopInteract(
            this,
            $"[{currentItem.itemName}]\n{GetStatText()}"
        );
    }
    private string GetStatText()
    {
        string desc = "";

        if (currentItem.damage != 0)
            desc += FormatStat("데미지", currentItem.damage);

        if (currentItem.moveSpeed != 0)
            desc += FormatStat("이동속도", currentItem.moveSpeed);

        if (currentItem.hp != 0)
            desc += FormatStat("체력", currentItem.hp);

        if (currentItem.bulletRate != 0)
            desc += FormatStat("공격속도", currentItem.bulletRate);

        return desc;
    }
    private string FormatStat(string statName, float value)
    {
        if (value > 0)
        {
            return $"{statName} <color=#7FFFF1>+{value}</color>\n";
        }
        else if (value < 0)
        {
            return $"{statName} <color=#FF7E85>{value}</color>\n";
        }

        return "";
    }
    public Transform GetTransform()
    {
        return transform;
    }
}