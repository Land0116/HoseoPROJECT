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

    /* public void OnPointerClick(PointerEventData eventData)
     {
         if (clickClip != null)
         {
             audioSource.PlayOneShot(clickClip);
         }
     }*/
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayIndependent(clickClip);
    }

    private void PlayIndependent(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        // AudioSource 복제용 오브젝트 생성
        GameObject obj = new GameObject("UI_SFX_TEMP");
        AudioSource newSource = obj.AddComponent<AudioSource>();

        // 기존 설정 그대로 복사 (핵심: SFX 볼륨 유지됨)
        newSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        newSource.volume = audioSource.volume;
        newSource.pitch = audioSource.pitch;
        newSource.spatialBlend = 0f; // UI니까 2D

        newSource.clip = clip;
        newSource.Play();

        Destroy(obj, clip.length);
    }
}