using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ItemUIManager : MonoBehaviour
{
    public static ItemUIManager Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Transform player;

    private List<ItemPickup> nearbyItems = new List<ItemPickup>();
    private List<WeaponPickup> nearbyWeapons = new List<WeaponPickup>();

    private object currentTarget; // ItemPickup 또는 WeaponPickup

    [SerializeField] private GameObject alertItemPanel;
    [SerializeField] private GameObject alertWeaponPanel;
    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    private void Update()
    {
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
                    panel.SetActive(false);

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
                    currentTarget = null;
                    panel.SetActive(false);
                    Destroy(weapon.gameObject);

                    if (alertWeaponPanel != null)
                        StartCoroutine(ShowAlertWeaponPaenl(1.5f));
                }
            }
        }
    }

    private void LateUpdate()
    {
        
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
            if (currentTarget is ItemPickup item) screenPos = Camera.main.WorldToScreenPoint(item.transform.position);
            else if (currentTarget is WeaponPickup weapon) screenPos = Camera.main.WorldToScreenPoint(weapon.transform.position);

            screenPos += new Vector3(250f, 0, 0);
            panel.transform.position = screenPos;
        }
    }

    private void UpdateUI(object target)
    {
        if (target == null) return;

        string desc = "";

        if (target is ItemPickup item)
        {
            var data = item.GetItemData();
            desc = $"[{data.itemName}]\n[F]키로 상호작용\n";
            if (data.damage != 0) desc += $"데미지 +{data.damage}\n";
            if (data.moveSpeed != 0) desc += $"이동속도 +{data.moveSpeed}\n";
            if (data.hp != 0) desc += $"체력 +{data.hp}\n";
            if (data.bulletRate != 0) desc += $"공격속도 +{data.bulletRate}\n";
        }
        else if (target is WeaponPickup weapon)
        {
            var data = weapon.GetWeaponData();
            desc = $"[{data.weaponName}]\n[F]키로 상호작용\n데미지 +{data.damage}\n";
        }

        text.text = desc;
        panel.SetActive(true);
    }

    // 등록 / 해제
    public void RegisterItem(ItemPickup item)
    {
        if (!nearbyItems.Contains(item)) nearbyItems.Add(item);
    }
    public void UnregisterItem(ItemPickup item)
    {
        nearbyItems.Remove(item);
        if ((currentTarget as UnityEngine.Object) == item)
        {
            currentTarget = null;
            panel.SetActive(false);
        }
    }

    public void RegisterWeapon(WeaponPickup weapon)
    {
        if (!nearbyWeapons.Contains(weapon)) nearbyWeapons.Add(weapon);
    }
    public void UnregisterWeapon(WeaponPickup weapon)
    {
        nearbyWeapons.Remove(weapon);
        if ((currentTarget as UnityEngine.Object) == weapon)
        {
            currentTarget = null;
            panel.SetActive(false);
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
}