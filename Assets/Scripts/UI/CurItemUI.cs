using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CurItemUI : MonoBehaviour
{
    public static CurItemUI Instance;

    [SerializeField] private Image itemImage;
    [Header("게임 시작 시 장착된 기본 아이템")]
    [SerializeField] private ItemData defaultItem;
    [SerializeField] private Image[] itemSlots;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
                itemSlots[i].enabled = false;
            }
        }
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

        // UI 갱신
        SetItems(PlayerController.Instance.GetEquippedItems());
        PlayerUIManager.Instance?.ForceRefreshPlayerUI();

    }

    public void OnHoverSlot(int index)
    {
        if (PlayerController.Instance == null) return;

        var items = PlayerController.Instance.GetEquippedItems();

        if (index >= items.Count) return;

        var data = items[index];
        if (data == null) return;

        // 슬롯 RectTransform 전달
        ItemUIManager.Instance.ShowUIItemInfo(data, itemSlots[index].rectTransform);
    }

    public void OnExitSlot()
    {
        ItemUIManager.Instance.HideUIItemInfo();
    }

}