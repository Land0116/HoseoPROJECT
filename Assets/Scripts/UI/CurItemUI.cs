using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class CurItemUI : MonoBehaviour
{
    public static CurItemUI Instance;

    [SerializeField] private Image itemImage;
    [Header("���� ���� �� ������ �⺻ ������")]
    [SerializeField] private ItemData defaultItem;
    [SerializeField] private Image[] itemSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].enabled = false;


            var slot = itemSlots[i].gameObject.AddComponent<ItemSlotUI>();
            slot.index = i;
        }
    }

    public void SetItems(List<ItemData> items)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < items.Count && items[i] != null && items[i].icon != null)
            {
                itemSlots[i].sprite = items[i].icon;
                itemSlots[i].enabled = true;
            }
            else
            {
                itemSlots[i].sprite = null;
                itemSlots[i].enabled = false;
            }
        }

        
        RefreshHoverAfterUpdate(items);
    }
    private void RefreshHoverAfterUpdate(List<ItemData> items)
    {
        if (ItemUIManager.Instance == null) return;

        // ���� ���콺 ��ġ UI Raycast�� �ٽ� ã��
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ItemSlotUI slot = result.gameObject.GetComponent<ItemSlotUI>();
            if (slot != null)
            {
                int index = slot.index;

                if (index >= 0 && index < items.Count)
                {
                    var data = items[index];
                    if (data != null)
                    {
                        ItemUIManager.Instance.ShowUIItemInfo(
                            data,
                            itemSlots[index].rectTransform
                        );
                    }
                }
                return;
            }
        }

        // ���� �� �ƴϸ� ���� ��
        ItemUIManager.Instance.HideUIItemInfo();
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnRightClickSlot(int index)
    {
        if (PlayerController.Instance == null) return;

        PlayerController.Instance.UnequipItem(index);

        var items = PlayerController.Instance.GetEquippedItems();

        SetItems(items);

        PlayerUIManager.Instance?.ForceRefreshPlayerUI();

        RefreshHoverAfterUpdate(items);
    }

    public void OnHoverSlot(int index)
    {
        if (PlayerController.Instance == null) return;

        var items = PlayerController.Instance.GetEquippedItems();

        if (index < 0 || index >= items.Count)
        {
            ItemUIManager.Instance.HideUIItemInfo();
            return;
        }

        var data = items[index];

        if (data == null)
        {
            ItemUIManager.Instance.HideUIItemInfo();
            return;
        }

        ItemUIManager.Instance.ShowUIItemInfo(data, itemSlots[index].rectTransform);
    }

    public void OnExitSlot()
    {
        ItemUIManager.Instance.HideUIItemInfo();
    }

}