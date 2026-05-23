using UnityEngine;

public class UIAudioPlayer : MonoBehaviour
{
    public static UIAudioPlayer Instance;

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip)
    {
        Debug.Log("Play »£√‚µ : " + clip);

        if (clip == null) return;

        audioSource.PlayOneShot(clip);
    }
}