using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button selectButton;

    private AugmentationSystem currentData;
    private AugUIManager currentManager;

    public void Setup(AugmentationSystem data, AugUIManager manager)
    {
        currentData = data;
        currentManager = manager;

        if (nameText != null)
            nameText.text = data.augmentationName;

        if (descText != null)
            descText.text = data.augmentationDesc;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClickAugmentation);
        }
    }

    public void OnClickAugmentation()
    {
        if (currentManager == null || currentData == null) return;
        currentManager.SelectAugmentation(currentData);
    }
}