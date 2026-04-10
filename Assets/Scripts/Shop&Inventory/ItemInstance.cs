[System.Serializable]
public class ItemInstance
{
    public ShopItemData data;

    public float lastUseTime = -999f;

    public ItemInstance(ShopItemData data)
    {
        this.data = data;
        lastUseTime = -999f;
    }
}