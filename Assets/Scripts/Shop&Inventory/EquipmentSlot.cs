using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using TMPro;
//using UnityEditorInternal.Profiling.Memory.Experimental;

public class EquipmentSlot : MonoBehaviour,
    IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image icon;
    GameObject dragIcon;
    Image dragImage;
    public enum SlotType { Q, E }
    public SlotType slotType;

    private Canvas canvas;
    private CanvasGroup canvasGroup;

    public bool isDropped = false;
    public ItemInstance item;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }



   

    public void OnDrop(PointerEventData eventData)
    {

        InventorySlot inv = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (inv != null && inv.item != null)
        {
            inv.isDropped = true;

            ItemInstance temp = item;

            SetItem(inv.item);
            inv.ClearItem();

            if (temp != null)
                InventoryManager.Instance.AddItem(temp);
        }

        EquipmentSlot equip = eventData.pointerDrag.GetComponent<EquipmentSlot>();

        if (equip != null && equip != this && equip.item != null)
        {
            equip.isDropped = true;

            ItemInstance temp = item;

            SetItem(equip.item);
            equip.SetItem(temp);
        }
    }

    public void SetItem(ItemInstance newItem)
    {
        item = newItem;

        UpdateUI();
        PlayerUIManager.Instance.SetEquipment(slotType, item);
    }

    public void ClearItem()
    {
        item = null;
        UpdateUI();

        PlayerUIManager.Instance.SetEquipment(slotType, null);
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
            icon.sprite = item.data.icon;
            icon.enabled = true;
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        isDropped = false;

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform);

        dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false;

        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = icon.rectTransform.sizeDelta;

        dragIcon.transform.position = eventData.position;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (dragIcon != null)
            Destroy(dragIcon);
    }

    public void ResetSlot()
    {
        item = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }
    public void TryUse()
    {
        if (item == null) return;

        if (item.data.effect == null)
        {
            Debug.Log("effect 없음");
            return;
        }

        if (Time.time < item.lastUseTime + item.data.cooldown)
        {
            Debug.Log("쿨타임 중");
            return;
        }

        item.data.effect.Use(PlayerController.Instance.gameObject);

        Debug.Log("아이템 사용: " + item.data.itemName);

        item.lastUseTime = Time.time;
    }
}