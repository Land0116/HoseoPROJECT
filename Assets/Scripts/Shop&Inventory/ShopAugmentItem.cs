using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ShopAugmentItem : MonoBehaviour, IInteractable, IShopInteractable
{
    [Header("데이터")]
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

        // UI 적용
        if (iconImage != null)
            iconImage.sprite = currentAugment.icon;

        RefreshPriceUI();
        if (priceText != null)
            priceText.text = "Aug\n" + GetFinalPrice().ToString() + "G";

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
        // 골드 부족
        if (player.Gold < finalPrice)
        {
            Debug.Log("골드 부족");
            PlayIndependent(purchaseFailSound);
            return;
        }

        // 구매 성공
        bool added = AugmentRunManager.Instance.TryAddAugment(currentAugment);

        if (!added)
        {
            Debug.Log("증강 추가 실패 (슬롯 가득 or 조건 불가)");
            
            return;
        }

        // 골드 차감
        player.Gold -= finalPrice;
        PlayIndependent(purchaseSuccessSound);
        // UI 갱신 (핵심)
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }

        Debug.Log("구매 성공: " + currentAugment.augmentationName);

        // 구매 후 비활성 or 재생성
        AfterPurchase();
    }

    private void AfterPurchase()
    {
        // 방법 1: 그냥 사라지게
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
            Debug.LogError("ItemUIManager가 아직 생성되지 않음");
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
            $"[{currentAugment.augmentationName}]\n{desc}"
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

        priceText.text = "Aug\n" + GetFinalPrice().ToString() + "G";
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