using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class InGameShopUIManager : MonoBehaviour
{
    public static InGameShopUIManager Instance;

    [SerializeField] private TMP_Text playerCurGold;

    [SerializeField] private Transform goodsParent; // 버튼들이 생성될 부모
    [SerializeField] private GameObject goodsButtonPrefab; // 버튼 프리팹

    [SerializeField] private Button closeButton;

    [Header("패널")]
    [SerializeField] private GameObject shopPanel;

    [Header("구매 버튼")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("상점 열기 제한")]
    public bool canOpenShop = true;
    private bool isOpen = false;

    [Header("아이템 데이터 ")]
    [SerializeField] private ShopItemData[] shopItems;

    


    
    [Header("상품설명")]
    [SerializeField] private Image descIcon;
    [SerializeField] private TMP_Text descName;
    [SerializeField] private TMP_Text descText;

    private ShopItemData selectedItem;

    private HashSet<ShopItemData> purchasedItems = new HashSet<ShopItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        
    }

    private void Start()
    {
        isOpen = false;
        shopPanel.SetActive(false);

        CreateShopButtons();

        buyButton.onClick.AddListener(OnClickBuy);
        closeButton.onClick.AddListener(CloseShop);
    }
    private void Update()
    {
        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseShop();
        }
    }



    public void ToggleShop()
    {
        if (!isOpen && !canOpenShop) return;

        isOpen = !isOpen;

        shopPanel.SetActive(isOpen);
        Cursor.visible = isOpen;
        Time.timeScale = isOpen ? 0f : 1f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControl(!isOpen);
        }
        if (isOpen)
        {
            UpdateGoldUI();
        }
    }
    
    //버튼 초기화


    //아이템 클릭시 정보 갱신
    private void OnClickItem(ShopItemData item)
    {
        selectedItem = item;

        descIcon.sprite = item.icon;
        descName.text = item.itemName;
        descText.text = item.description;

        UpdateBuyButtonState();
    }
    public void BindShopUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform shopRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "InGameShopUIPanel");
        if (shopRoot != null)
        {
            shopPanel = shopRoot.gameObject;
            Debug.Log("InGameShopPanel 연결됨");
        }
        else
        {
            Debug.LogError("InGameShopPanel 못찾음");
        }



        Transform descIconRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "GoodsDesc_Icon");
        if (descIconRoot != null)
            descIcon = descIconRoot.GetComponent<Image>();

        Transform descNameRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "GoodsDesc_Title");
        if (descNameRoot != null)
            descName = descNameRoot.GetComponent<TMP_Text>();

        Transform descTextRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "GoodsDesc");
        if (descTextRoot != null)
            descText = descTextRoot.GetComponent<TMP_Text>();


    }
    public void UpdateGoldUI()
    {
        if (PlayerController.Instance == null) return;

        playerCurGold.text = PlayerController.Instance.Gold + " G";
    }

    private void UpdateBuyButtonState()
    {
        if (selectedItem == null) return;

        // 이미 구매한 경우
        if (purchasedItems.Contains(selectedItem))
        {
            buyButtonText.text = "보유중";
            buyButton.interactable = false;
            return;
        }

        // 구매 가능
        buyButtonText.text = "구매";
        buyButton.interactable = true;
    }

    private void OnClickBuy()
    {
        Debug.Log("구매 버튼 눌림");
        if (selectedItem == null) return;

        // 중복 구매 방지
        if (purchasedItems.Contains(selectedItem))
        {
            Debug.Log("이미 보유중");
            return;
        }

        int playerGold = PlayerController.Instance.Gold;

        // 골드 부족
        if (playerGold < selectedItem.price)
        {
            Debug.Log("골드 부족");
            return;
        }

        //  구매 성공
        PlayerController.Instance.Gold -= selectedItem.price;

        //  보유 목록 추가
        purchasedItems.Add(selectedItem);

        
        InventoryManager.Instance.AddItem(new ItemInstance(selectedItem));
        Debug.Log("구매 완료: " + selectedItem.itemName);

        //  UI 갱신
        UpdateGoldUI();
        UpdateBuyButtonState();
    }
    private void CreateShopButtons()
    {
        for (int i = 0; i < shopItems.Length; i++)
        {
            GameObject btnObj = Instantiate(goodsButtonPrefab, goodsParent);

            Button btn = btnObj.GetComponent<Button>();

            Image icon = btnObj.transform.Find("Icon").GetComponent<Image>();
            TMP_Text price = btnObj.transform.Find("PriceText").GetComponent<TMP_Text>();

            ShopItemData item = shopItems[i];

            // UI 적용
            icon.sprite = item.icon;
            price.text = item.price + " G";

            // 클릭 이벤트
            btn.onClick.AddListener(() => OnClickItem(item));
        }
    }

    public void ResetShop()
    {
        purchasedItems.Clear();
        // 버튼 UI 초기화
        buyButtonText.text = "구매";
        buyButton.interactable = true;

        // 설명창 초기화 
        descIcon.sprite = null;
        descName.text = "";
        descText.text = "";
    }
    public void CloseShop()
    {
        if (!isOpen) return;

        isOpen = false;
        
        shopPanel.SetActive(false);
        

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControl(true);
        }
    }
    public bool IsShopOpen()
    {
        return isOpen;
    }
}
