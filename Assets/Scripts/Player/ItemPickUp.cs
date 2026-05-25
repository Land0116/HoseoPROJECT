using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private float itemRenderSize = 20f;
    [SerializeField] private ItemData itemData;
    public ItemData GetItemData() => itemData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        //Debug.Log("Item Enter");

        //var itemUI = ItemUIManager.Instance;
        var itemUI = UIManager.Instance?.GetWeaponUI();
       // Debug.Log(itemUI == null ? "itemUI null임" : "itemUI 정상");
        if (itemUI != null)
        {
            //Debug.Log("RegisterItem 호출됨");
            itemUI.RegisterItem(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        //Debug.Log("Item Exit 발생");
        var itemUI = UIManager.Instance?.GetWeaponUI();

        if (itemUI != null)
        {
            itemUI.UnregisterItem(this);
        }
    }

    public void SetItemData(ItemData data)
    {
        itemData = data;

        if (iconRenderer != null && itemData != null)
        {
            iconRenderer.sprite = itemData.icon;
            SetFixedPixelSize(iconRenderer, itemRenderSize);
        }
    }
    private void SetFixedPixelSize(SpriteRenderer renderer, float targetPixelSize)
    {
        if (renderer.sprite == null) return;

        Sprite sprite = renderer.sprite;

        // 원본 픽셀 크기
        float width = sprite.rect.width;
        float height = sprite.rect.height;

        // PPU (Pixels Per Unit)
        float ppu = sprite.pixelsPerUnit;

        // 현재 월드 크기 계산
        float worldWidth = width / ppu;
        float worldHeight = height / ppu;

        float maxWorldSize = Mathf.Max(worldWidth, worldHeight);

        // 목표 월드 크기 (20픽셀 → 월드 변환)
        float targetWorldSize = targetPixelSize / ppu;

        float scale = targetWorldSize / maxWorldSize;

        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}