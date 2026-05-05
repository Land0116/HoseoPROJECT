using UnityEngine;
public class ShopDropItem : MonoBehaviour
{
    /*private ItemData itemData;

    public ItemData GetItemData() => itemData;

    [SerializeField] private SpriteRenderer sr;

    public void SetItem(ItemInstance instance)
    {
        SetItemDirect(instance.data);
    }

    public void Interact(PlayerController player)
    {
        if (itemData == null) return;

        bool success = player.EquipItem(itemData);

        if (success)
        {
            CurItemUI.Instance?.SetItems(player.GetEquippedItems());
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var itemUI = UIManager.Instance?.GetWeaponUI();

        if (itemUI != null)
        {
            itemUI.RegisterItem(GetComponent<ItemPickup>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var itemUI = UIManager.Instance?.GetWeaponUI();

        if (itemUI != null)
        {
            itemUI.UnregisterItem(GetComponent<ItemPickup>());
        }
    }
    public void SetItemDirect(ItemData data)
    {
        itemData = data;

        if (sr == null)
        {
            Debug.LogError("SpriteRenderer 없음");
            return;
        }

        sr.sprite = itemData.icon;
        sr.enabled = true;

        Debug.Log($"[ShopDropItem] 생성 아이템 = {itemData.itemName}");
    }*/
}