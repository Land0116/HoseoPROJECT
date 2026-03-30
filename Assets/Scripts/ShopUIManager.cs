using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    [Header("전체 아이템 데이터")]
    public ItemData[] itemDatabase;

    [Header("UI")]
    public GameObject uiPanel;
    public ShopButton[] uiButtons;

    public bool isSelectedAug = false;

    /// <summary>
    /// UIManager에서 씬 로드 후 호출됨 (Aug 방식 그대로)
    /// </summary>
    public void BindShopUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform shopPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "ShopUIPanel");

        if (shopPanelRoot != null)
        {
            uiPanel = shopPanelRoot.gameObject;
            uiButtons = shopPanelRoot.GetComponentsInChildren<ShopButton>(true);
        }
    }

    public void ShowShop()
    {
        if (itemDatabase == null || itemDatabase.Length == 0) return;
        if (uiPanel == null) return;
        if (uiPanel.activeSelf) return;

        Time.timeScale = 0f;
        uiPanel.SetActive(true);

        List<ItemData> randomItems = itemDatabase
            .OrderBy(x => Random.value)
            .Take(3)
            .ToList();

        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (i < randomItems.Count)
            {
                ItemData item = randomItems[i];
                int price = GetPriceByItem(item);

                uiButtons[i].Setup(item, price, this);
                uiButtons[i].gameObject.SetActive(true);
            }
            else
            {
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private int GetPriceByItem(ItemData item)
    {
        switch (item.zoneType)
        {
            case ItemZoneType.TutoZone: return Random.Range(1, 4);
            case ItemZoneType.StorageZone: return Random.Range(3, 6);
            case ItemZoneType.SortZone: return Random.Range(5, 8);
            case ItemZoneType.NormalZone: return Random.Range(7, 10);
            case ItemZoneType.PremiumZone: return Random.Range(9, 12);
        }
        return 1;
    }

    public void TryBuyItem(ItemData item, int price)
    {
        if (PlayerController.Instance == null) return;

        if (PlayerController.Instance.Gold < price)
        {
            Debug.Log("돈 부족");
            return;
        }

        PlayerController.Instance.Gold -= price;
        PlayerController.Instance.EquipItem(item);

        if (CurItemUI.Instance != null)
        {
            CurItemUI.Instance.SetItem(item);
        }

        CloseShop();
    }

    public void CloseShop()
    {
        if (uiPanel != null)
        {
            Time.timeScale = 1f;
            uiPanel.SetActive(false);
        }
    }
}