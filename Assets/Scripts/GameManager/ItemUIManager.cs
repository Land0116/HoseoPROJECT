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

    private void Awake()
    {
        Instance = this;

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
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (player == null && playerController != null)
            player = playerController.transform;

        if (Keyboard.current.fKey.wasPressedThisFrame && currentTarget != null && PlayerController.Instance != null)
        {
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

                    currentTarget = null;
                    if (itemPanel != null) itemPanel.SetActive(false);
                }
            }
            else if (currentTarget is WeaponPickup weapon)
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
            Vector3 screenPos = Vector3.zero;

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
            if (data.damage != 0) desc += $"데미지 +{data.damage}\n";
            if (data.moveSpeed != 0) desc += $"이동속도 +{data.moveSpeed}\n";
            if (data.hp != 0) desc += $"체력 +{data.hp}\n";
            if (data.bulletRate != 0) desc += $"공격속도 +{data.bulletRate}\n";

            itemText.text = desc;
            itemPanel.SetActive(true);
        }
        else if (target is WeaponPickup weapon)
        {
            var data = weapon.GetWeaponData();

            string desc = $"[{data.weaponName}]\n[F]키로 상호작용\n데미지 +{data.damage}\n";

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

        string desc = $"[{data.itemName}]\n";
        if (data.damage != 0) desc += $"데미지 +{data.damage}\n";
        if (data.moveSpeed != 0) desc += $"이동속도 +{data.moveSpeed}\n";
        if (data.hp != 0) desc += $"체력 +{data.hp}\n";
        if (data.bulletRate != 0) desc += $"공격속도 +{data.bulletRate}\n";

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

        uiItemRect.anchoredPosition = localPoint + new Vector2(1150f, 550f);
    }

    public void HideUIItemInfo()
    {
        if (uiItemPanel != null)
            uiItemPanel.SetActive(false);
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
}