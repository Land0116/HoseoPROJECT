using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopAugmentItem : MonoBehaviour, IInteractable, IShopInteractable
{
    [Header("������")]
    [SerializeField] private AugmentationSystem[] augmentPool;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip purchaseSuccessSound;
    [SerializeField] private AudioClip purchaseFailSound;

    private AugmentationSystem currentAugment;
    private int price;

    private bool isPlayerInRange;
    private PlayerController player;

    private void Start()
    {
        GenerateItem();
    }
    private void Update()
    {
        if (currentAugment == null) return;
        RefreshPriceUI();
    }

    private void GenerateItem()
    {
        if (augmentPool == null || augmentPool.Length == 0) return;

        int rand = Random.Range(0, augmentPool.Length);
        currentAugment = augmentPool[rand];

        if (currentAugment == null) return;

        price = currentAugment.goldCost;

        // UI ����
        if (iconImage != null)
            iconImage.sprite = currentAugment.icon;

        RefreshPriceUI();
        if (priceText != null)
            priceText.text = "" + GetFinalPrice().ToString() + "G";

    }

    
    public void Interact(PlayerController player)
    {
        this.player = player;
        TryPurchase();
    }
    private void TryPurchase()
    {
        if (player == null) return;

        int finalPrice = GetFinalPrice();

        // =========================
        // 1. �� ����
        // =========================
        if (player.Gold < finalPrice)
        {
            Debug.Log("��� ����");

            if (ItemUIManager.Instance != null)
            {
                ItemUIManager.Instance.ShowAlertMessage("골드가 부족합니다.", false);
            }

            PlayIndependent(purchaseFailSound);
            return;
        }

        // =========================
        // 2. ���� �߰� �õ�
        // =========================
        bool added = AugmentRunManager.Instance.TryAddAugment(currentAugment);

        if (!added)
        {
            Debug.Log("���� �߰� ���� (���� ����)");

            if (ItemUIManager.Instance != null)
            {
                ItemUIManager.Instance.ShowAlertMessage("증강이 최대입니다.", false);
            }

            PlayIndependent(purchaseFailSound);
            return;
        }

        // =========================
        // 3. ���� ����
        // =========================
        player.Gold -= finalPrice;

        if (ItemUIManager.Instance != null)
        {
            ItemUIManager.Instance.ShowAlertMessage("구매에 성공하였습니다!", false);
        }

        PlayIndependent(purchaseSuccessSound);

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }

        Debug.Log("���� ����: " + currentAugment.augmentationName);

        AfterPurchase();
    }

    private void AfterPurchase()
    {
        // ��� 1: �׳� �������
        gameObject.SetActive(false);

        
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
        if (ItemUIManager.Instance == null)
        {
            Debug.LogError("ItemUIManager�� ���� �������� ����");
            return;
        }

        /*if (currentAugment == null) return;

        ItemUIManager.Instance.ShowShopInteract(
            this,
            $"[{currentAugment.augmentationName}]\n{currentAugment.augmentationDesc}"
        );*/
        if (currentAugment == null) return;

        string desc = GetAugmentDescription(currentAugment);

        ItemUIManager.Instance.ShowShopInteract(
            this,
            currentAugment
        );
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
        if (currentAugment == null) return;

        priceText.text = "" + GetFinalPrice().ToString() + "G";
    }
    private string GetAugmentDescription(AugmentationSystem augment)
    {
        if (augment == null) return "";

        switch (augment.category)
        {
            case AugmentationSystem.AugmentCategory.Special:
                var special = augment.GetSpecialLevelData(1);
                if (special != null && !string.IsNullOrEmpty(special.ruleDescription))
                    return special.ruleDescription;
                break;

            case AugmentationSystem.AugmentCategory.Passive:
                var passive = augment.GetPassiveLevelData(1);
                if (passive != null && !string.IsNullOrEmpty(passive.levelDescription))
                    return passive.levelDescription;
                break;

            case AugmentationSystem.AugmentCategory.SubSkill:
                var sub = augment.GetSubSkillLevelData(1);
                if (sub != null && !string.IsNullOrEmpty(sub.levelDescription))
                    return sub.levelDescription;
                break;
        }

        return augment.augmentationDesc;
    }
    private void PlayIndependent(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        GameObject obj = new GameObject("Shop_SFX_TEMP");
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