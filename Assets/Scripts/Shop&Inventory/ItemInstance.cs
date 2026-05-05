using System;

[Serializable]
public class ItemInstance
{
    public ItemData data;

    public ItemInstance(ItemData data)
    {
        this.data = data;
    }
}