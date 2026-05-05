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
        if (!isPlayerInRange) return;
        if (player == null) return;

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

        if (priceText != null)
            priceText.text = price.ToString();
    }

    public void Interact(PlayerController player)
    {
        if (!isPlayerInRange) return;
        if (player == null) return;
        if (currentAugment == null) return;

        this.player = player;

        TryPurchase();
    }

    private void TryPurchase()
    {
        if (player == null) return;

        // 골드 부족
        if (player.Gold < price)
        {
            Debug.Log("골드 부족");
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
        player.Gold -= price;

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
        if (currentAugment == null) return;

        ItemUIManager.Instance.ShowShopInteract(
            this,
            $"[{currentAugment.augmentationName}]\n{currentAugment.augmentationDesc}"
        );
    }
    public Transform GetTransform()
    {
        return transform;
    }
}