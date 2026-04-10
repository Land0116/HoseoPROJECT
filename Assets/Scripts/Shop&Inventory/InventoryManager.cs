using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;


public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("패널 ")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button inventoryCloseBtn;

    [Header("아이템 슬롯")]
    [SerializeField] private Transform goodsIndex;
    [SerializeField] private GameObject slotPrefab;

    [Header("아이템 설명")]
    [SerializeField] private Image descIcon;
    [SerializeField] private TMP_Text descName;
    [SerializeField] private TMP_Text descText;

    

    [Header("열기 제한")]
    public bool canOpenInventory = true;
    private bool isOpen = false;
    private List<ItemInstance> items = new List<ItemInstance>();

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
        inventoryPanel.SetActive(false);

    }


    public void BindInventoryUI(GameObject systemUIRoot)
    {
        Debug.Log("BindInventoryUI 실행됨");

        Transform root = UIManager.FindChildRecursive(systemUIRoot.transform, "InventoryPanel");
        if (root != null)
            inventoryPanel = root.gameObject;

        goodsIndex = UIManager.FindChildRecursive(systemUIRoot.transform, "GoodsIndex");
        if (goodsIndex == null)
            Debug.LogError("GoodsIndex 못찾음");


        descIcon = UIManager.FindChildRecursive(systemUIRoot.transform, "DescIcon").GetComponent<Image>();
        descName = UIManager.FindChildRecursive(systemUIRoot.transform, "DescName").GetComponent<TMP_Text>();
        descText = UIManager.FindChildRecursive(systemUIRoot.transform, "DescText").GetComponent<TMP_Text>();
        Transform closeBtnTr = UIManager.FindChildRecursive(systemUIRoot.transform, "InventoryCloseBtn");
        if (closeBtnTr != null)
        {
            inventoryCloseBtn = closeBtnTr.GetComponent<Button>();

            if (inventoryCloseBtn != null)
            {
                inventoryCloseBtn.onClick.RemoveAllListeners(); // 중복 방지
                inventoryCloseBtn.onClick.AddListener(CloseInventory);
            }
            else
            {
                Debug.LogError("InventoryCloseBtn에 Button 컴포넌트 없음");
            }
        }
        else
        {
            Debug.LogError("InventoryCloseBtn 못찾음");
        }
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        ToggleInventory();
    }
    public void ShowItemDesc(ItemInstance item)
    {
        descIcon.sprite = item.data.icon;
        descName.text = item.data.itemName;
        descText.text = item.data.description;
    }

    public void AddItem(ItemInstance item)
    {
        items.Add(item);

        GameObject slotObj = Instantiate(slotPrefab, goodsIndex);

        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot == null)
        {
            Debug.LogError("InventorySlot 없음");
            return;
        }

        slot.SetItem(item);
    }
    private void OnClickItem(ShopItemData item)
    {
        descIcon.sprite = item.icon;
        descName.text = item.itemName;
        descText.text = item.description;
    }

    public void ToggleInventory()
    {
        if (!isOpen && !canOpenInventory) return;

        isOpen = !isOpen;

        inventoryPanel.SetActive(isOpen);

        Cursor.visible = isOpen;

        Time.timeScale = isOpen ? 0f : 1f;


        if (PlayerController.Instance != null)
            PlayerController.Instance.SetControl(!isOpen);

        // 크로스헤어 같이 처리
        if (PlayerController.Instance != null)
        {
            var cross = PlayerController.Instance.GetCrosshairTransform();
            if (cross != null)
                cross.gameObject.SetActive(!isOpen);
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }
    public void ResetInventory()
    {
        Debug.Log("인벤토리 초기화");

        items.Clear();

        // 슬롯 UI 전부 삭제
        foreach (Transform child in goodsIndex)
        {
            Destroy(child.gameObject);
        }

        // 설명창 초기화
        descIcon.sprite = null;
        descName.text = "";
        descText.text = "";
    }
    
}
