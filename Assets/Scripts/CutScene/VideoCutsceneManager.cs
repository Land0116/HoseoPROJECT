using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoCutsceneManager : MonoBehaviour
{
    public static VideoCutsceneManager Instance { get; private set; }

    private const string CutsceneInputLockKey = "VideoCutscene";

    private class VideoCutsceneRequest
    {
        public VideoCutsceneType cutsceneType;
        public int stageNumber;
        public bool forceReplay;
        public Action onFinished;
    }

    [Header("컷씬 루트")]
    [SerializeField] private GameObject cutsceneRoot;

    [Header("컷씬 화면")]
    [SerializeField] private RawImage cutsceneRawImage;
    [SerializeField] private RenderTexture renderTexture;

    [Header("비디오 플레이어")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;

    [Header("컷씬 데이터 목록")]
    [SerializeField] private VideoCutsceneData[] cutsceneDatas;

    private readonly Queue<VideoCutsceneRequest> requestQueue = new Queue<VideoCutsceneRequest>();
    private readonly HashSet<string> playedKeys = new HashSet<string>();

    private Coroutine playCoroutine;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        AutoBind();
        SetupVideoPlayer();
        HideCutscenePanel();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBind();
    }

    private void Reset()
    {
        AutoBind();
    }
#endif

    private void AutoBind()
    {
        if (cutsceneRoot == null)
        {
            Transform panel = FindChildRecursive(transform, "CutscenePanel");

            if (panel == null)
                panel = FindChildRecursive(transform, "BossCutscenePanel");

            if (panel != null)
                cutsceneRoot = panel.gameObject;
        }

        if (cutsceneRawImage == null)
        {
            cutsceneRawImage = GetComponentInChildren<RawImage>(true);
        }

        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>(true);
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == targetName)
                return child;

            Transform found = FindChildRecursive(child, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;

        if (renderTexture != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            if (cutsceneRawImage != null)
            {
                cutsceneRawImage.texture = renderTexture;
            }
        }

        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
    }

    public void PlayCutscene(
        VideoCutsceneType cutsceneType,
        int stageNumber = 0,
        Action onFinished = null,
        bool forceReplay = false)
    {
        if (cutsceneType == VideoCutsceneType.None)
        {
            onFinished?.Invoke();
            return;
        }

        requestQueue.Enqueue(new VideoCutsceneRequest
        {
            cutsceneType = cutsceneType,
            stageNumber = stageNumber,
            forceReplay = forceReplay,
            onFinished = onFinished
        });

        if (!isPlaying)
        {
            playCoroutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;

        while (requestQueue.Count > 0)
        {
            VideoCutsceneRequest request = requestQueue.Dequeue();
            VideoCutsceneData data = GetCutsceneData(request.cutsceneType, request.stageNumber);

            if (data == null)
            {
                Debug.LogWarning(
                    "[VideoCutsceneManager] 컷씬 데이터를 찾을 수 없음: " +
                    request.cutsceneType +
                    " / Stage: " +
                    request.stageNumber
                );

                request.onFinished?.Invoke();
                continue;
            }

            string playKey = GetPlayKey(data, request.stageNumber);

            if (data.playOnce &&
                !request.forceReplay &&
                playedKeys.Contains(playKey))
            {
                request.onFinished?.Invoke();
                continue;
            }

            if (data.playOnce)
            {
                playedKeys.Add(playKey);
            }

            yield return PlayCutsceneRoutine(data, request);
        }

        isPlaying = false;
        playCoroutine = null;
    }

    private IEnumerator PlayCutsceneRoutine(VideoCutsceneData data, VideoCutsceneRequest request)
    {
        if (data.lockPlayerInput)
        {
            //PlayerController.Instance?.SetSystemInputLockedByKey(CutsceneInputLockKey, true);
        }

        ShowCutscenePanel();
        BringCutsceneToFront();

        if (videoPlayer == null || data.videoClips == null || data.videoClips.Length == 0)
        {
            Debug.LogWarning("[VideoCutsceneManager] VideoPlayer 또는 VideoClip이 없음");
            FinishCutscene(data, request);
            yield break;
        }

        for (int i = 0; i < data.videoClips.Length; i++)
        {
            VideoClip clip = data.videoClips[i];

            if (clip == null)
                continue;

            yield return PlaySingleClip(clip, data);
        }

        if (data.finishDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(data.finishDelay);
        }

        FinishCutscene(data, request);
    }

    private IEnumerator PlaySingleClip(VideoClip clip, VideoCutsceneData data)
    {
        videoPlayer.Stop();

        if (audioSource != null)
        {
            audioSource.Stop();
        }

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
            if (data.allowSkip && IsSkipPressed(data.skipKey))
            {
                break;
            }

            yield return null;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.Stop();

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private bool IsSkipPressed(Key key)
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void FinishCutscene(VideoCutsceneData data, VideoCutsceneRequest request)
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        HideCutscenePanel();

        if (data != null && data.lockPlayerInput)
        {
            //PlayerController.Instance?.SetSystemInputLockedByKey(CutsceneInputLockKey, false);
        }

        request.onFinished?.Invoke();
    }

    private VideoCutsceneData GetCutsceneData(VideoCutsceneType cutsceneType, int stageNumber)
    {
        VideoCutsceneData fallbackCommonData = null;

        if (cutsceneDatas == null)
            return null;

        for (int i = 0; i < cutsceneDatas.Length; i++)
        {
            VideoCutsceneData data = cutsceneDatas[i];

            if (data == null)
                continue;

            if (data.cutsceneType != cutsceneType)
                continue;

            if (data.stageNumber == stageNumber)
                return data;

            if (data.stageNumber == 0)
                fallbackCommonData = data;
        }

        return fallbackCommonData;
    }

    private string GetPlayKey(VideoCutsceneData data, int requestedStageNumber)
    {
        int finalStageNumber = data.stageNumber > 0
            ? data.stageNumber
            : requestedStageNumber;

        return data.cutsceneType + "_Stage_" + finalStageNumber;
    }

    public void ShowCutscenePanel()
    {
        if (cutsceneRoot != null)
        {
            cutsceneRoot.SetActive(true);
            cutsceneRoot.transform.SetAsLastSibling();
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
            cutsceneRoot.SetActive(true);
            cutsceneRoot.transform.SetAsLastSibling();
        }
    }

    public void ClearPlayedCutscenes()
    {
        playedKeys.Clear();
    }
    public IEnumerator PlayCutsceneForExternalFade(
        VideoCutsceneType cutsceneType,
        int stageNumber = 0,
        bool forceReplay = false)
    {
        VideoCutsceneData data = GetCutsceneData(cutsceneType, stageNumber);

        if (data == null)
        {
            Debug.LogWarning("[VideoCutsceneManager] 컷씬 데이터를 찾을 수 없음: " + cutsceneType);
            yield break;
        }

        string playKey = GetPlayKey(data, stageNumber);

        if (data.playOnce && !forceReplay && playedKeys.Contains(playKey))
        {
            yield break;
        }

        if (data.playOnce)
        {
            playedKeys.Add(playKey);
        }

        if (data.lockPlayerInput)
        {
            //PlayerController.Instance?.SetSystemInputLockedByKey("VideoCutscene", true);
        }

        ShowCutscenePanel();
        BringCutsceneToFront();

        if (videoPlayer == null || data.videoClips == null || data.videoClips.Length == 0)
        {
            Debug.LogWarning("[VideoCutsceneManager] VideoPlayer 또는 VideoClip이 없음");
            yield break;
        }

        for (int i = 0; i < data.videoClips.Length; i++)
        {
            VideoClip clip = data.videoClips[i];

            if (clip == null)
                continue;

            yield return PlaySingleClipForExternalFade(clip, data);
        }

        if (data.finishDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(data.finishDelay);
        }
    }
    private IEnumerator PlaySingleClipForExternalFade(VideoClip clip, VideoCutsceneData data)
    {
        videoPlayer.Stop();

        if (audioSource != null)
        {
            audioSource.Stop();
        }

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
            if (data.allowSkip && IsSkipPressed(data.skipKey))
            {
                break;
            }

            yield return null;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;

        // 바로 Stop 하지 않는다.
        // 페이드아웃이 끝날 때까지 마지막 화면을 유지하기 위함.
        videoPlayer.Pause();

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    public void EndExternalFadeCutscene()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        HideCutscenePanel();

        //PlayerController.Instance?.SetSystemInputLockedByKey("VideoCutscene", false);
    }
}