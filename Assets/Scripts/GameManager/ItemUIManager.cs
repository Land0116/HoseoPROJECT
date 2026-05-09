using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ItemUIManager : MonoBehaviour
{
    public static ItemUIManager Instance;

    [SerializeField] private GameObject itemPanel;
    [SerializeField] private TextMeshProUGUI itemText;

    [SerializeField] private GameObject weaponPanel;
    [SerializeField] private TextMeshProUGUI weaponText;

    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;

    private List<ItemPickup> nearbyItems = new List<ItemPickup>();
    private List<WeaponPickup> nearbyWeapons = new List<WeaponPickup>();

    private object currentTarget;

    [SerializeField] private GameObject alertItemPanel;
    [SerializeField] private GameObject alertWeaponPanel;
    [SerializeField] private GameObject alertMaxItemPanel;

    [SerializeField] private GameObject uiItemPanel;
    [SerializeField] private TextMeshProUGUI uiItemText;

    private RectTransform uiItemRect;
    private RectTransform canvasRect;

    [SerializeField] private GameObject itemInteractPanel; //*
    [SerializeField] private TextMeshProUGUI itemInteractText; //*
    private Transform currentInteractTarget;
    private string currentInteractName;
    private List<BagInteractable> nearbyBags = new List<BagInteractable>();
       
    //상점 상호작용 UI
    [SerializeField] private GameObject shopInteractPanel;
    [SerializeField] private TextMeshProUGUI shopInteractText;
    private List<IShopInteractable> nearbyShops = new List<IShopInteractable>();
    private IShopInteractable currentShopTarget;
    private bool isShopUIForced = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (itemInteractPanel != null) itemInteractPanel.SetActive(false); //*
        if (itemPanel != null) itemPanel.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (uiItemPanel != null)
        {
            uiItemPanel.SetActive(false);
            uiItemRect = uiItemPanel.GetComponent<RectTransform>();
            canvasRect = uiItemPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        }

        playerController = FindFirstObjectByType<PlayerController>();

        if (playerController != null)
            player = playerController.transform;
    }

    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame && PlayerController.Instance != null)
        {

            if (currentShopTarget != null)
            {
                currentShopTarget.Interact(PlayerController.Instance);

                // 상점은 1회 클릭 후 유지 원하면 이 줄 제거
                return;
            }


            if (currentTarget is ItemPickup item)
            {
                var itemData = item.GetItemData();
                if (itemData != null)
                {
                    bool success = PlayerController.Instance.EquipItem(itemData);

                    if (success)
                    {
                        CurItemUI.Instance.SetItems(PlayerController.Instance.GetEquippedItems());

                        nearbyItems.Remove(item);
                        Destroy(item.gameObject);

                        currentTarget = null;
                        if (itemPanel != null) itemPanel.SetActive(false);

                        if (alertItemPanel != null)
                            StartCoroutine(ShowAlertItemPaenl(1.5f));
                    }
                    else
                    {
                        if (alertMaxItemPanel != null)
                            StartCoroutine(ShowAlertMaxItemPanel(1.5f));
                    }
                }

                return;
            }


            if (currentTarget is WeaponPickup weapon)
            {
                var weaponData = weapon.GetWeaponData();
                if (weaponData != null)
                {
                    PlayerController.Instance.EquipWeapon(weaponData);
                    CurWeaponUI.Instance.SetWeapon(weaponData);

                    nearbyWeapons.Remove(weapon);
                    Destroy(weapon.gameObject);

                    currentTarget = null;
                    if (weaponPanel != null) weaponPanel.SetActive(false);

                    if (alertWeaponPanel != null)
                        StartCoroutine(ShowAlertWeaponPaenl(1.5f));
                }

                return;
            }
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
                player = playerController.transform;
        }

        if (player == null) return;

        float minDist = float.MaxValue;
        object nearest = null;

        foreach (var item in nearbyItems)
        {
            float dist = Vector2.Distance(player.position, item.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = item;
            }
        }

        foreach (var weapon in nearbyWeapons)
        {
            float dist = Vector2.Distance(player.position, weapon.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = weapon;
            }
        }

        if (nearest != currentTarget)
        {
            currentTarget = nearest;
            UpdateUI(currentTarget);
        }

        if (currentTarget != null && Camera.main != null)
        {
            Vector3 screenPos;

            if (currentTarget is ItemPickup item)
            {
                screenPos = Camera.main.WorldToScreenPoint(item.transform.position);
                itemPanel.transform.position = screenPos + new Vector3(-250f, 0, 0);
            }
            else if (currentTarget is WeaponPickup weapon)
            {
                screenPos = Camera.main.WorldToScreenPoint(weapon.transform.position);
                weaponPanel.transform.position = screenPos + new Vector3(-250f, 0, 0);
            }
        }

        float minShopDist = float.MaxValue;
        IShopInteractable nearestShop = null;

        foreach (var shop in nearbyShops)
        {
            if (shop == null) continue;

            float dist = Vector2.Distance(
                player.position,
                shop.GetTransform().position
            );

            if (dist < minShopDist)
            {
                minShopDist = dist;
                nearestShop = shop;
            }
        }

        currentShopTarget = nearestShop;

        if (currentShopTarget == null && !isShopUIForced)
        {
            shopInteractPanel.SetActive(false);
        }

        UpdateShopInteractUI();
        UpdateInteractTarget();
        UpdateInteractPanel();
    }


    private void UpdateUI(object target)
    {
        if (itemPanel != null) itemPanel.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);

        if (target == null) return;

        if (target is ItemPickup item)
        {
            var data = item.GetItemData();
            if (data == null) return;

            string desc = $"[{data.itemName}]\n[F]키로 상호작용\n";

            if (!string.IsNullOrEmpty(data.description))
            {
                desc += $"{data.description}\n";
            }

            if (data.damage != 0)
                desc += FormatStat("데미지", data.damage);

            if (data.moveSpeed != 0)
                desc += FormatStat("이동속도", data.moveSpeed);

            if (data.hp != 0)
                desc += FormatStat("체력", data.hp);

            if (data.bulletRate != 0)
                desc += FormatStat("공격속도", data.bulletRate);

            itemText.text = desc;
            itemPanel.SetActive(true);
        }
        else if (target is WeaponPickup weapon)
        {
            var data = weapon.GetWeaponData();
            if (data == null) return;

            string desc = $"[{data.weaponName}]\n[F]키로 상호작용\n";

            desc += FormatStat("데미지", data.damage);

            weaponText.text = desc;
            weaponPanel.SetActive(true);
        }
    }

    public void RegisterItem(ItemPickup item)
    {
        if (!nearbyItems.Contains(item))
            nearbyItems.Add(item);
    }

    public void UnregisterItem(ItemPickup item)
    {
        nearbyItems.Remove(item);
    }

    public void RegisterWeapon(WeaponPickup weapon)
    {
        if (!nearbyWeapons.Contains(weapon))
            nearbyWeapons.Add(weapon);
    }

    public void UnregisterWeapon(WeaponPickup weapon)
    {
        nearbyWeapons.Remove(weapon);
    }


    public void ShowUIItemInfo(ItemData data, RectTransform slotRect)
    {
        if (data == null || uiItemPanel == null) return;

        string desc = $"[우클릭 시 장착 해제]\n[{data.itemName}]\n";

        if (!string.IsNullOrEmpty(data.description))
        {
            desc += $"{data.description}\n";
        }

        if (data.damage != 0)
            desc += FormatStat("데미지", data.damage);

        if (data.moveSpeed != 0)
            desc += FormatStat("이동속도", data.moveSpeed);

        if (data.hp != 0)
            desc += FormatStat("체력", data.hp);

        if (data.bulletRate != 0)
            desc += FormatStat("공격속도", data.bulletRate);

        uiItemText.text = desc;
        uiItemPanel.SetActive(true);

        uiItemRect.anchoredPosition = Vector2.zero;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, slotRect.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        uiItemRect.anchoredPosition = localPoint + new Vector2(200f, 0f);
    }

    public void HideUIItemInfo()
    {
        if (uiItemPanel != null)
            uiItemPanel.SetActive(false);
    }
    public void ShowInteractPanel(Transform target, string name)
    {
        if (itemInteractPanel == null || Camera.main == null) return;

        itemInteractText.text = $"[{name}]\n[F]키로 상호작용";

        RectTransform interactRect = itemInteractPanel.GetComponent<RectTransform>();

        
        Vector3 worldOffsetPos = target.position + new Vector3(2f, 0f, 0f);
        

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            worldOffsetPos
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        interactRect.anchoredPosition = localPoint;

        itemInteractPanel.SetActive(true);
    }
    public void HideInteractPanel()
    {
        if (itemInteractPanel != null)//*
            itemInteractPanel.SetActive(false);
    }
    public void SetInteractTarget(Transform target, string name)
    {
        currentInteractTarget = target;
        currentInteractName = name;
    }
    private System.Collections.IEnumerator ShowAlertItemPaenl(float duration)
    {
        alertItemPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        alertItemPanel.SetActive(false);
    }

    private System.Collections.IEnumerator ShowAlertWeaponPaenl(float duration)
    {
        alertWeaponPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        alertWeaponPanel.SetActive(false);
    }

    private System.Collections.IEnumerator ShowAlertMaxItemPanel(float duration)
    {
        if (alertMaxItemPanel == null) yield break;

        alertMaxItemPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        alertMaxItemPanel.SetActive(false);
    }

    public void BindItemUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform interactRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "ItemInteractPanel");
        if (interactRoot != null)
        {
            itemInteractPanel = interactRoot.gameObject; //*
            itemInteractText = interactRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Transform alertMaxItemRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AlertMaxItemPanel");
        if (alertMaxItemRoot != null)
            alertMaxItemPanel = alertMaxItemRoot.gameObject;

        Transform itemPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "ItemUIPanel");
        if (itemPanelRoot != null)
        {
            itemPanel = itemPanelRoot.gameObject;
            itemText = itemPanelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Transform weaponPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "WeaponUIPanel");
        if (weaponPanelRoot != null)
        {
            weaponPanel = weaponPanelRoot.gameObject;
            weaponText = weaponPanelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Transform alertItemRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AlertItemPanel");
        if (alertItemRoot != null)
            alertItemPanel = alertItemRoot.gameObject;

        Transform alertWeaponRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AlertWeaponPanel");
        if (alertWeaponRoot != null)
            alertWeaponPanel = alertWeaponRoot.gameObject;

        if (itemPanel != null) itemPanel.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (alertItemPanel != null) alertItemPanel.SetActive(false);
        if (alertWeaponPanel != null) alertWeaponPanel.SetActive(false);
        if (alertMaxItemPanel != null) alertMaxItemPanel.SetActive(false);

        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
            player = playerController.transform;

        currentTarget = null;
        nearbyItems.Clear();
        nearbyWeapons.Clear();
    }
    private string FormatStat(string statName, float value)
    {
        if (value > 0)
        {
            return $"{statName} <color=#4DA3FF>+{value}</color>\n";
        }
        else if (value < 0)
        {
            return $"{statName} <color=#FF3B3B>{value}</color>\n";
        }

        return "";
    }

    private void UpdateInteractPanel()
    {
        if (itemInteractPanel == null || Camera.main == null)
            return;

        if (currentInteractTarget == null)
        {
            itemInteractPanel.SetActive(false);
            return;
        }

        itemInteractText.text = $"[{currentInteractName}]\n[F]키로 상호작용";

        RectTransform interactRect = itemInteractPanel.GetComponent<RectTransform>();

        Vector3 worldOffsetPos = currentInteractTarget.position + new Vector3(2f, 0f, 0f);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            worldOffsetPos
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        interactRect.anchoredPosition = localPoint;

        if (!itemInteractPanel.activeSelf)
            itemInteractPanel.SetActive(true);
    }
    public void RegisterBag(BagInteractable bag)
    {
        if (!nearbyBags.Contains(bag))
            nearbyBags.Add(bag);
    }

    public void UnregisterBag(BagInteractable bag)
    {
        nearbyBags.Remove(bag);
    }
    private void UpdateInteractTarget()
    {
        if (player == null) return;

        float minDist = float.MaxValue;
        BagInteractable nearestBag = null;

        foreach (var bag in nearbyBags)
        {
            if (bag == null) continue;

            float dist = Vector2.Distance(player.position, bag.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearestBag = bag;
            }
        }

        if (nearestBag == null)
        {
            currentInteractTarget = null;
            return;
        }

        currentInteractTarget = nearestBag.transform;
        currentInteractName = "[아이템 보따리]";
    }

    public void ShowShopInteract(IShopInteractable shop, string text)
    {
        if (shopInteractPanel == null || shop == null) return;

        currentShopTarget = shop;
        isShopUIForced = true;

        shopInteractText.text = text;

        if (!shopInteractPanel.activeSelf)
            shopInteractPanel.SetActive(true);
    }
    private void UpdateShopInteractUI()
    {
        if (shopInteractPanel == null || Camera.main == null)
            return;

        if (!isShopUIForced && currentShopTarget == null)
        {
            shopInteractPanel.SetActive(false);
            return;
        }

        if (currentShopTarget == null)
            return;

        RectTransform rect = shopInteractPanel.GetComponent<RectTransform>();

        Vector3 worldPos = currentShopTarget.GetTransform().position + new Vector3(-3f, 0f, 0f);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            worldPos
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        rect.anchoredPosition = localPoint;

        if (!shopInteractPanel.activeSelf)
            shopInteractPanel.SetActive(true);
    }
    public void HideShopInteract()
    {
        currentShopTarget = null;
        isShopUIForced = false;

        if (shopInteractPanel != null)
            shopInteractPanel.SetActive(false);
    }
    public void RegisterShop(IShopInteractable shop)
    {
        if (!nearbyShops.Contains(shop))
            nearbyShops.Add(shop);
    }

    public void UnregisterShop(IShopInteractable shop)
    {
        nearbyShops.Remove(shop);
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
