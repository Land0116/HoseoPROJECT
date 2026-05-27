using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonHoverSprite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;

    [Header("Sprites")]
    [SerializeField] private Sprite noHoverSprite;
    [SerializeField] private Sprite hoverSprite;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        targetImage.sprite = noHoverSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSprite != null)
            targetImage.sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (noHoverSprite != null)
            targetImage.sprite = noHoverSprite;
    }
}