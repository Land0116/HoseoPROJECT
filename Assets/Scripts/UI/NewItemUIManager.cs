using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NewItemUIManager : MonoBehaviour
{
    public static NewItemUIManager Instance;
    
    [Header("전체 아이템 데이터")]
    public ItemData[] itemDatabase;

    [Header("특수 아이템 데이터")]
    public ItemData[] specialitemDatabase;

    [Header("basic Item")]
    [SerializeField] private ItemData[] firstItemDatabase;
    [SerializeField] private ItemData[] secondItemDatabase;
    [SerializeField] private ItemData[] thirdItemDatabase;

    [Header("special Item")]
    [SerializeField] private ItemData[] firstSpecialItemDatabase;
    [SerializeField] private ItemData[] secondSpecialItemDatabase;
    [SerializeField] private ItemData[] thirdSpecialItemDatabase;

    [Header("UI")]
    public GameObject uiPanel;
    public ShopButton[] uiButtons;

    public GameObject itemPickupPrefab;

    private bool isOpenedItem = false;

    public bool IsOpenedItem
    {
        get => isOpenedItem;
        set
        {
            if (isOpenedItem == value) return;

            isOpenedItem = value;

            if (isOpenedItem)
            {
                SpawnRandomItems(); //* 05.08
                //ShowShop();
                isOpenedItem = false;
            }
        }
    }
    
    public void SpawnItemRewardFromReward(Action rewardFinishedCallback)
    {
        Debug.Log("[NewItemUIManager] 아이템 보상 드랍 시작");
        SpawnRandomItems();

        rewardFinishedCallback?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    /// <summary>
    /// UIManager에서 씬 로드 후 호출 (Aug 방식 그대로)
    /// </summary>
    public void BindShopUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform shopPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "ItemSelectUIPanel");

        if (shopPanelRoot != null)
        {
            uiPanel = shopPanelRoot.gameObject;
            uiButtons = shopPanelRoot.GetComponentsInChildren<ShopButton>(true);
        }
    }

    public void ShowShop()
    {
        Debug.Log($"itemDatabase: {itemDatabase?.Length}, uiPanel: {uiPanel}");
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
            case ItemZoneType.TutoZone: return Random.Range(0, 1);
            case ItemZoneType.StorageZone: return Random.Range(0, 1);
            case ItemZoneType.SortZone: return Random.Range(0, 1);
            case ItemZoneType.NormalZone: return Random.Range(0, 1);
            case ItemZoneType.PremiumZone: return Random.Range(0, 1);
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

        bool success = PlayerController.Instance.EquipItem(item);

        if (success)
        {
            // 정상 장착 → UI 갱신
            if (CurItemUI.Instance != null)
            {
                CurItemUI.Instance.SetItems(
                    PlayerController.Instance.GetEquippedItems()
                );
            }
        }
        else
        {
            // 슬롯 꽉참 → 플레이어 밑에 드랍
            SpawnItemUnderPlayer(item);
        }

        CloseShop();
    }
    private void SpawnItemUnderPlayer(ItemData item)
    {
        if (itemPickupPrefab == null)
        {
            Debug.LogWarning("ItemPickup 프리팹 없음");
            return;
        }

        if (PlayerController.Instance == null) return;

        Vector3 spawnPos = PlayerController.Instance.transform.position;

        GameObject obj = Instantiate(itemPickupPrefab, spawnPos, Quaternion.identity);

        ItemPickup pickup = obj.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.SetItemData(item);
        }
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

    
    private void SpawnRandomItems()
    {
        if (itemPickupPrefab == null)
        {
            Debug.LogWarning("[NewItemUIManager] itemPickupPrefab이 없음. 아이템 드랍 불가.");
            return;
        }

        if (PlayerController.Instance == null)
        {
            Debug.LogWarning("[NewItemUIManager] PlayerController.Instance가 없음. 아이템 드랍 불가.");
            return;
        }

        GetCurrentStageItemDB(out ItemData[] normalDB, out ItemData[] specialDB);

        if ((normalDB == null || normalDB.Length == 0) &&
            (specialDB == null || specialDB.Length == 0))
        {
            Debug.LogWarning("[NewItemUIManager] 아이템 DB가 비어 있음. 아이템 드랍 불가.");
            return;
        }

        int itemCount = Random.value < 0.75f ? 2 : 3;

        float[] xPositions = itemCount == 2
            ? new float[] { -1.0f, 1.0f }
            : new float[] { -2.0f, 0.0f, 2.0f };

        Vector3 basePos = PlayerController.Instance.transform.position;

        for (int i = 0; i < itemCount; i++)
        {
            ItemData item = RollItem();

            if (item == null)
            {
                Debug.LogWarning("[NewItemUIManager] RollItem 결과가 null임.");
                continue;
            }

            Vector3 spawnPos = basePos + new Vector3(xPositions[i], 0f, 0f);

            GameObject obj = Instantiate(itemPickupPrefab, spawnPos, Quaternion.identity);

            ItemPickup pickup = obj.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.SetItemData(item);
            }
            else
            {
                Debug.LogWarning("[NewItemUIManager] itemPickupPrefab에 ItemPickup 컴포넌트가 없음.");
            }
        }
    }
   
    
    private ItemData RollItem()
    {
        PlayerController player = PlayerController.Instance;

        float baseChance = 0.1f; // 10%
        float bonus = 0f;

        if (player != null)
        {
            bonus = player.GetSpecialChanceAdd();
        }

        float finalChance = baseChance + (bonus / 100f);
        finalChance = Mathf.Clamp01(finalChance);

        bool isSpecial = Random.value < finalChance;

        
        GetCurrentStageItemDB(out ItemData[] normalDB, out ItemData[] specialDB);//*

        if (isSpecial && specialDB != null && specialDB.Length > 0)
        {
            return specialDB[Random.Range(0, specialDB.Length)];
        }

        return normalDB[Random.Range(0, normalDB.Length)];
    }

    private void GetCurrentStageItemDB(out ItemData[] normalDB, out ItemData[] specialDB)
    {
        normalDB = itemDatabase;
        specialDB = specialitemDatabase;

        int stage = 1;

        if (MapFlowManager.Instance != null)
        {
            stage = MapFlowManager.Instance.CurrentAct;
        }
        //else if (StageClear.Instance != null)
        //{
        //    stage = StageClear.Instance.GetCurrentStageNumber();
        //}

        switch (stage)
        {
            case 1:
                if (firstItemDatabase != null && firstItemDatabase.Length > 0)
                    normalDB = firstItemDatabase;

                if (firstSpecialItemDatabase != null && firstSpecialItemDatabase.Length > 0)
                    specialDB = firstSpecialItemDatabase;
                break;

            case 2:
                if (secondItemDatabase != null && secondItemDatabase.Length > 0)
                    normalDB = secondItemDatabase;

                if (secondSpecialItemDatabase != null && secondSpecialItemDatabase.Length > 0)
                    specialDB = secondSpecialItemDatabase;
                break;

            case 3:
                if (thirdItemDatabase != null && thirdItemDatabase.Length > 0)
                    normalDB = thirdItemDatabase;

                if (thirdSpecialItemDatabase != null && thirdSpecialItemDatabase.Length > 0)
                    specialDB = thirdSpecialItemDatabase;
                break;
        }
    }
}