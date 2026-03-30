using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    public ItemData GetItemData() => itemData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Debug.Log("Item Enter");

        //var itemUI = ItemUIManager.Instance;
        var itemUI = UIManager.Instance?.GetWeaponUI();
        Debug.Log(itemUI == null ? "itemUI null임" : "itemUI 정상");
        if (itemUI != null)
        {
            Debug.Log("RegisterItem 호출됨");
            itemUI.RegisterItem(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        Debug.Log("Item Exit 발생");
        var itemUI = UIManager.Instance?.GetWeaponUI();

        if (itemUI != null)
        {
            itemUI.UnregisterItem(this);
        }
    }
}