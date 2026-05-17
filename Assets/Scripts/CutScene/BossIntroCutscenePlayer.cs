using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class BossIntroCutscenePlayer : MonoBehaviour
{
    [Header("컷씬 루트")]
    [SerializeField] private GameObject cutsceneRoot;

    [Header("비디오 플레이어")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("순차 재생할 영상")]
    [SerializeField] private VideoClip[] videoClips = new VideoClip[4];

    public void ShowCutscenePanel()
    {
        if (cutsceneRoot != null)
        {
            cutsceneRoot.SetActive(true);
        }
    }

    public void HideCutscenePanel()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (cutsceneRoot != null)
        {
            cutsceneRoot.SetActive(false);
        }
    }

    public void BringCutsceneToFront()
    {
        if (cutsceneRoot != null)
        {
            cutsceneRoot.transform.SetAsLastSibling();
        }
    }

    public IEnumerator PlayCutsceneSequence()
    {
        ShowCutscenePanel();

        if (videoPlayer == null || videoClips == null || videoClips.Length == 0)
        {
            Debug.LogWarning("[BossIntroCutscenePlayer] VideoPlayer 또는 VideoClip이 없음");
            yield break;
        }

        for (int i = 0; i < videoClips.Length; i++)
        {
            VideoClip clip = videoClips[i];

            if (clip == null) continue;

            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;

            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            bool finished = false;

            void OnVideoFinished(VideoPlayer source)
            {
                finished = true;
            }

            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();

            while (!finished)
            {
                yield return null;
            }

            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.Stop();
        }
    }
}