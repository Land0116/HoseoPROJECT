using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public Image icon;
    public ShopItemData item;

    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector3 originalPosition;

    public bool isDropped = false;

    GameObject dragIcon;
    Image dragImage;
    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetItem(ShopItemData newItem)
    {
        item = newItem;
        UpdateUI();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        isDropped = false;

        //드래그 이미지
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform);

        dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false;

       //크기 맞추기
        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = icon.rectTransform.sizeDelta;

        dragIcon.transform.position = eventData.position;

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnDrop(PointerEventData eventData)
    {
        
        InventorySlot dragged = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (dragged != null && dragged.item != null)
        {
            dragged.isDropped = true;

            ShopItemData temp = item;
            item = dragged.item;
            dragged.item = temp;

            UpdateUI();
            dragged.UpdateUI();
            return;
        }


        EquipmentSlot equip = eventData.pointerDrag.GetComponent<EquipmentSlot>();

        if (equip != null && equip.item != null)
        {
            equip.isDropped = true;

            // 기존 아이템 있으면 교환
            ShopItemData temp = item;

            SetItem(equip.item);

            //  장착 슬롯 비우기 (이게 핵심)
            equip.ClearItem();

            // 기존 인벤 아이템 다시 넣기
            if (temp != null)
            {
                InventoryManager.Instance.AddItem(temp);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (dragIcon != null)
            Destroy(dragIcon);

    }

    private void UpdateUI()
    {
        if (item == null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        else
        {
            icon.sprite = item.icon;
            icon.enabled = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null) return;

        InventoryManager.Instance.ShowItemDesc(item);
    }

    public void ClearItem()
    {
        item = null;
        UpdateUI();
    }
}