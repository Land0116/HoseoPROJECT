using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int index;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CurItemUI.Instance.OnRightClickSlot(index);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CurItemUI.Instance.OnHoverSlot(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CurItemUI.Instance.OnExitSlot();
    }
}