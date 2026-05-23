using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioSource audioSource;

    [Header("Sounds")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverClip != null)
        {
            audioSource.PlayOneShot(hoverClip);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickClip != null)
        {
            audioSource.PlayOneShot(clickClip);
        }
    }
}