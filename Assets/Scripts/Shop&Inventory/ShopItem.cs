using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopItem : MonoBehaviour, IInteractable, IShopInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip purchaseSuccessSound;
    [SerializeField] private AudioClip purchaseFailSound;

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

    [Header("���� ������")]
    [SerializeField] private Sprite potion25Icon;
    [SerializeField] private Sprite potion50Icon;
    [SerializeField] private Sprite potion75Icon;
    [Header("potion percent")]
    [SerializeField, Range(0f, 1f)] private float potionChance = 0.2f; // 20%

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
        if (currentItem == null) return;
        RefreshPriceUI();
    }

    // =========================
    // Interact (���� �ٽ�)
    // =========================
    public void Interact(PlayerController player)
    {
        if (player == null) return;

        int finalPrice = GetFinalPrice();

        // =========================
        // 1. �� ���� �� ���� ����
        // =========================
        if (player.Gold < finalPrice)
        {
            ItemUIManager.Instance?.ShowAlertMessage("골드가 부족합니다.", false);
            PlayIndependent(purchaseFailSound);
            return;
        }

        // =========================
        // 2. ���� ���
        // =========================
        if (!isPotion && currentItem == null)
            return;

        // =========================
        // 3. ��� ���� (���� Ȯ��)
        // =========================
        player.Gold -= finalPrice;

        // =========================
        // 4. ������ ����
        // =========================
        if (isPotion)
        {
            ApplyPotion(player);
        }
        else
        {
            bool equipSuccess = player.EquipItem(currentItem);

            if (!equipSuccess)
            {
                // �κ� ���� �� �ٴ� ��� (UI�� �� ���)
                SpawnWorldItem(currentItem);
            }
            else
            {
                CurItemUI.Instance?.SetItems(player.GetEquippedItems());
            }
        }

        // =========================
        // 5. ���� ���� (���� �޽���)
        // =========================
        ItemUIManager.Instance?.ShowAlertMessage("구매성공!", false);
        PlayIndependent(purchaseSuccessSound);

        gameObject.SetActive(false);
    }

    // =========================
    // ������ ���� (�ٽ� ����)
    // =========================
    private void ApplyItem(PlayerController player)
    {
        ItemData itemToGive = currentItem;

        bool success = player.EquipItem(itemToGive);

        if (!success)
        {
            Debug.Log("[ShopItem] �κ��丮 ������ �� ������ ���� ����");
            PlayIndependent(purchaseSuccessSound);
            SpawnWorldItem(itemToGive);
        }
        else
        {
            CurItemUI.Instance?.SetItems(player.GetEquippedItems());
        }
    }

    // =========================
    // ���� ���
    // =========================
    private void SpawnWorldItem(ItemData item)
    {
        if (worldItemPrefab == null)
        {
            Debug.LogError("worldItemPrefab ����");
            return;
        }

        GameObject obj = Instantiate(worldItemPrefab, transform.position, Quaternion.identity);

        ItemPickup pickup = obj.GetComponent<ItemPickup>();

        if (pickup == null)
        {
            Debug.LogError("ItemPickup ���� (������ Ȯ��)");
            return;
        }

        pickup.SetItemData(item);

        Debug.Log($"[ShopItem] ������ ��� ���� = {item.itemName}");
    }

    // =========================
    // ����
    // =========================
    private void ApplyPotion(PlayerController player)
    {
        float healAmount = player.MaxHp * potionHealPercent;
        player.Heal(healAmount);
    }

    // =========================
    // ������ ����
    // =========================
    public void GenerateItem()
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

        if (Random.value < potionChance)
        {
            isPotion = true;
            currentItem = null;

            int potionType = Random.Range(0, 3);

            if (potionType == 0) { potionHealPercent = 0.25f; price = 25; }
            else if (potionType == 1) { potionHealPercent = 0.5f; price = 50; }
            else { potionHealPercent = 0.75f; price = 100; }

            UpdateUI();
            return;
        }

        bool isPremium = RollPremium();

        if (!isPremium && normalPool != null && normalPool.Length > 0)
        {
            currentItem = normalPool[Random.Range(0, normalPool.Length)];
            isPotion = false;
            price = Random.Range(75, 86);
        }
        else if (premiumPool != null && premiumPool.Length > 0)
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

        RefreshPriceUI();
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
    /*private void ShowUI()
    {
        if (ItemUIManager.Instance == null)
        {
            Debug.LogError("ItemUIManager�� ���� �������� ����");
            return;
        }

        if (isPotion)
        {
            string name = "";
            string desc = "";

            if (potionHealPercent == 0.25f)
            {
                name = "�ϱ� ü�� ȸ�� ����";
                desc = "���� ü���� 25% ȸ���Ѵ�.";
            }
            else if (potionHealPercent == 0.5f)
            {
                name = "�߱� ü�� ȸ�� ����";
                desc = "���� ü���� 50% ȸ���Ѵ�.";
            }
            else if (potionHealPercent == 0.75f)
            {
                name = "��� ü�� ȸ�� ����";
                desc = "���� ü���� 75% ȸ���Ѵ�.";
            }

            ItemUIManager.Instance.ShowShopInteract(
                this,
                $"[{name}]\n{desc}"
            );

            return;
        }

        // �Ϲ� �������� ��
        if (currentItem == null) return;

        ItemUIManager.Instance.ShowShopInteract(
            this,
            $"[{currentItem.itemName}]\n{GetStatText()}" //*
        );

    }*/
    /*private void ShowUI()
    {
        if (ItemUIManager.Instance == null)
        {
            Debug.LogError("ItemUIManager�� ���� �������� ����");
            return;
        }

        if (isPotion)
        {
            string name = "";
            string desc = "";

            if (potionHealPercent == 0.25f)
            {
                name = "�ϱ� ü�� ȸ�� ����";
                desc = "���� ü���� 25% ȸ���Ѵ�.";
            }
            else if (potionHealPercent == 0.5f)
            {
                name = "�߱� ü�� ȸ�� ����";
                desc = "���� ü���� 50% ȸ���Ѵ�.";
            }
            else if (potionHealPercent == 0.75f)
            {
                name = "��� ü�� ȸ�� ����";
                desc = "���� ü���� 75% ȸ���Ѵ�.";
            }

            /*ItemUIManager.Instance.ShowShopInteract( //* ���� �ʿ�
                this,
                $"[{name}]\n{desc}"
            );

            return;
        }

        if (currentItem == null) return;

        string descText = $"[{currentItem.itemName}]\n";


        if (!string.IsNullOrEmpty(currentItem.description))
        {
            descText += $"{currentItem.description}\n";
        }

        descText += GetStatText();

        /*ItemUIManager.Instance.ShowShopInteract( //* ���� �ʿ�
            this,
            descText
        );
    }*/
    private void ShowUI()
    {
        if (ItemUIManager.Instance == null)
        {
            Debug.LogError("ItemUIManager�� ���� �������� ����");
            return;
        }

        if (isPotion)
        {
            string title = "";
            string category = "회복 아이템";
            string effect = "";
            string desc = "";

            if (potionHealPercent == 0.25f)
            {
                title = "소형포션";
                effect = "25%";
                desc = "체력을 25%회복합니다..";
            }
            else if (potionHealPercent == 0.5f)
            {
                title = "중형포션";
                effect = "50%";
                desc = "체력을 50%회복합니다.";
            }
            else if (potionHealPercent == 0.75f)
            {
                title = "대형포션";
                effect = "75%";
                desc = "체력을 75%회복합니다.";
            }

            ItemUIManager.Instance.ShowShopInteract(this, null);
            ItemUIManager.Instance.ShowPotionTooltip(title, category, effect, desc);

            return;
        }

        if (currentItem == null) return;

        ItemUIManager.Instance.ShowShopInteract(this, null); // ��ġ UI Ȱ��ȭ
        ItemUIManager.Instance.ShowShopItemTooltip(currentItem); // �ٽ� ����
    }

    private string GetStatText()
    {
        string desc = "";

        

        if (currentItem.damage != 0)
            desc += FormatStat("������", currentItem.damage);

        if (currentItem.moveSpeed != 0)
            desc += FormatStat("�̵��ӵ�", currentItem.moveSpeed);

        if (currentItem.hp != 0)
            desc += FormatStat("ü��", currentItem.hp);

        if (currentItem.bulletRate != 0)
            desc += FormatStat("���ݼӵ�", currentItem.bulletRate);

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
    private int GetFinalPrice()
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return price;

        float discount = p.GetShopDiscountFromItems();
        return Mathf.RoundToInt(price * (1f - discount));
    }
    private void RefreshPriceUI()
    {
        if (priceText == null) return;
        priceText.text = "" + GetFinalPrice().ToString() + "G";
    }

    private bool RollPremium()
    {
        PlayerController player = PlayerController.Instance;

        float basePremiumChance = 0.2f; // ���� (���� 20%)

        float bonus = 0f;

        if (player != null)
        {
            bonus = player.GetSpecialChanceAdd();
        }

        float final = basePremiumChance + (bonus / 100f);
        return Random.value < Mathf.Clamp01(final);
    }

    private void PlayIndependent(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        GameObject obj = new GameObject("ShopItem_SFX_TEMP");
        AudioSource newSource = obj.AddComponent<AudioSource>();

        newSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        newSource.volume = sfxSource.volume;
        newSource.pitch = sfxSource.pitch;
        newSource.spatialBlend = 0f;

        newSource.clip = clip;
        newSource.Play();

        Destroy(obj, clip.length);
    }
}