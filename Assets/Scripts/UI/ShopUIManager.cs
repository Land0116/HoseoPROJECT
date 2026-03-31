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
    /// UIManager에서 씬 로드 후 호출 (Aug 방식 그대로)
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
        
        uiPanel.SetActive(true);
        Time.timeScale = 0f;
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
    public bool IsShopVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }
    
    public void HideCurrentShopUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }
    
    /// <summary>
    /// ESC 패널을 닫았을 때 상점 UI를 다시 보여줌
    /// 이미 뽑혀 있던 아이템 버튼 상태를 그대로 유지함
    /// </summary>
    public void RestoreCurrentShopUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(true);

        // 상점 UI는 열려 있는 동안 게임이 멈춰 있어야 함
        Time.timeScale = 0f;
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