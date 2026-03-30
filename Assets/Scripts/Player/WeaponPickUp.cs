using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    public WeaponData GetWeaponData() => weaponData;

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (!collision.CompareTag("Player")) return;

        Debug.Log("Item Enter");
        
        var itemUI = UIManager.Instance?.GetItemUI();

        if (itemUI != null)
        {
            itemUI.RegisterWeapon(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var itemUI = UIManager.Instance?.GetItemUI();

        if (itemUI != null)
        {
            itemUI.UnregisterWeapon(this);
        }
    }
}