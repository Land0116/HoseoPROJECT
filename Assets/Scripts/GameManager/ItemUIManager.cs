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

    private void Awake()
    {
        Instance = this;

        if (itemPanel != null) itemPanel.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);

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
                    PlayerController.Instance.EquipItem(itemData);
                    CurItemUI.Instance.SetItem(itemData);

                    nearbyItems.Remove(item);
                    Destroy(item.gameObject);

                    currentTarget = null;
                    if (itemPanel != null) itemPanel.SetActive(false);

                    if (alertItemPanel != null)
                        StartCoroutine(ShowAlertItemPaenl(1.5f));
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
            Debug.Log("currentTarget 변경됨: " + currentTarget);
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
        Debug.Log("UpdateUI 호출됨: " + target);
        if (itemPanel != null) itemPanel.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);

        if (target == null)
        {
            Debug.Log("target null이라 리턴");
            return;
        }

        if (target is ItemPickup item)
        {

            Debug.Log("Item UI 실행됨");

            var data = item.GetItemData();

            if (data == null)
            {
                Debug.Log("itemData null임");
                return;
            }


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

    // 등록 / 해제
    public void RegisterItem(ItemPickup item)
    {
        Debug.Log("RegisterItem 들어옴");
        if (!nearbyItems.Contains(item))
        {
            nearbyItems.Add(item);
            Debug.Log("아이템 리스트 추가됨: " + nearbyItems.Count);
        }
    }

    public void UnregisterItem(ItemPickup item)
    {
        nearbyItems.Remove(item);

        if (currentTarget is ItemPickup targetItem && targetItem == item)
        {
            currentTarget = null;
            if (itemPanel != null) itemPanel.SetActive(false);
        }
    }

    public void RegisterWeapon(WeaponPickup weapon)
    {
        if (!nearbyWeapons.Contains(weapon)) nearbyWeapons.Add(weapon);
    }

    public void UnregisterWeapon(WeaponPickup weapon)
    {
        nearbyWeapons.Remove(weapon);

        if (currentTarget is WeaponPickup targetWeapon && targetWeapon == weapon)
        {
            currentTarget = null;
            if (weaponPanel != null) weaponPanel.SetActive(false);
        }
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

    public void BindItemUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform itemPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "ItemUIPanel");
        if (itemPanelRoot != null)
        {
            itemPanel = itemPanelRoot.gameObject;
            itemText = itemPanelRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            //Debug.Log("ItemPanel 연결됨");
        }
        else
        {
            //Debug.LogError("ItemUIPanel 못찾음");
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

        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
            player = playerController.transform;

        currentTarget = null;
        nearbyItems.Clear();
        nearbyWeapons.Clear();
    }
}