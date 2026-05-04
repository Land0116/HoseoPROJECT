using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button selectButton;

    private ItemData currentData;
    private int currentPrice;
    private NewItemUIManager manager;

    public void Setup(ItemData data, int price, NewItemUIManager manager)
    {
        if (data == null || manager == null) return;
        this.currentData = data;
        this.currentPrice = price;
        this.manager = manager;

        nameText.text = data.itemName;
        iconImage.sprite = data.icon;
        iconImage.enabled = data.icon != null;

        string desc = "";
        if (data.damage != 0) desc += $"데미지 +{data.damage}\n";
        if (data.moveSpeed != 0) desc += $"이동속도 +{data.moveSpeed}\n";
        if (data.hp != 0) desc += $"체력 +{data.hp}\n";
        if (data.bulletRate != 0) desc += $"공속 +{data.bulletRate}\n";

        descText.text = desc;
        priceText.text = $"{price} G";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);
    }
    private void OnClick()
    {
        if (manager == null || currentData == null) return;

        manager.TryBuyItem(currentData, currentPrice);
    }
}
