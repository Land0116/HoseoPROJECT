using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BGMSoundManager : MonoBehaviour
{
    public static BGMSoundManager Instance;

    private string lastSceneName;
    private bool blockNextResume = false;
    private BGMType lastBGMType;
    private bool isCutscenePlaying = false;

    private Coroutine bossBGMCoroutine;

    [Header("Shop BGM")]
    [SerializeField] private AudioClip shopSound;
    [SerializeField] private AudioClip shopOpenSound;
    [SerializeField] private float shopOpenDelay = 0f;



    [SerializeField][Range(0f, 1f)] private float shopSoundVolume = 0.5f;

    private bool shopOpenPlayed = false;

    private Coroutine shopCoroutine;
    private float shopBaseVolume = 0.5f;

    private enum BGMType
    {
        Main,
        Normal,
        Shop,
        Boss
    }

    [System.Serializable]
    private class SceneBGMData
    {
        public string sceneName;
        public BGMType bgmType;
        public int stageNumber;
    }

    [System.Serializable]
    private class StageBGM
    {
        public int stageNumber;
        public AudioClip normalBGM;
        public AudioClip bossBGM;

        public float bossBGMDelay; // 핵심: 여기로 이동
    }

    [Header("씬별 BGM 설정")]
    [SerializeField] private SceneBGMData[] sceneDatas;

    [Header("메인 BGM")]
    [SerializeField] private AudioClip mainBGM;

    [Header("스테이지별 BGM")]
    [SerializeField] private StageBGM[] stageBGMs;

    [SerializeField] private AudioSource bgmSource;

    private int currentStage = -1;
    private BGMType currentType;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }
    private void Start()
    {
        ApplyBGM(SceneManager.GetActiveScene().name);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBGM(scene.name);
    }

    private void ApplyBGM(string sceneName)
    {
        if (isCutscenePlaying)
        {
            Debug.Log("[BGM] 컷씬 중 → 재생 차단");
            return;
        }

        lastSceneName = sceneName;

        SceneBGMData data = GetSceneData(sceneName);

        if (data == null)
        {
            Debug.LogError($"[BGM] 씬 데이터 없음: {sceneName}");
            return;
        }

        StageBGM stageData = GetStageBGM(data.stageNumber);

        if (stageData == null)
        {
            Debug.LogError($"[BGM] 스테이지 데이터 없음: {data.stageNumber}");
            return;
        }

        StopShopIfNeeded(data.bgmType);

        if (data.bgmType == BGMType.Main)
        {
            shopOpenPlayed = false;
        }

        switch (data.bgmType)
        {
            case BGMType.Main:
                currentType = BGMType.Main;
                currentStage = data.stageNumber;

                PlayClip(mainBGM);
                return;

            case BGMType.Normal:
                currentType = BGMType.Normal;
                currentStage = data.stageNumber;

                if (bgmSource.clip != stageData.normalBGM)
                {
                    bgmSource.Stop();
                    bgmSource.clip = stageData.normalBGM;
                    bgmSource.loop = true;
                    bgmSource.Play();
                }
                else
                {
                    if (!bgmSource.isPlaying)
                        bgmSource.Play();
                }

                return;

            case BGMType.Shop:
                if (shopSound != null)
                {
                    bgmSource.clip = shopSound;
                    bgmSource.loop = true;
                    bgmSource.volume = shopBaseVolume;
                    bgmSource.Play();
                }

                if (!shopOpenPlayed && shopOpenSound != null)
                {
                    shopOpenPlayed = true;
                    StartCoroutine(PlayShopOpenOnce(shopOpenDelay));
                }

                currentType = BGMType.Shop;
                currentStage = data.stageNumber;
                return;

            case BGMType.Boss:
                if (bossBGMCoroutine != null)
                    StopCoroutine(bossBGMCoroutine);

                bossBGMCoroutine = StartCoroutine(
                    PlayBossBGMWithDelay(stageData.bossBGM, stageData.bossBGMDelay)
                );

                currentType = BGMType.Boss;
                currentStage = data.stageNumber;
                return;
        }
    }
    private IEnumerator PlayBossBGMWithDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isCutscenePlaying)
        {
            PlayClip(clip);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (bgmSource.clip == clip)
        {
            if (!bgmSource.isPlaying)
                bgmSource.Play();
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private SceneBGMData GetSceneData(string sceneName)
    {
        foreach (var data in sceneDatas)
        {
            if (data.sceneName == sceneName)
                return data;
        }
        return null;
    }

    private StageBGM GetStageBGM(int stage)
    {
        foreach (var data in stageBGMs)
        {
            if (data.stageNumber == stage)
                return data;
        }
        return null;
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void ForceApplyBGM(string sceneName)
    {
        ApplyBGM(sceneName);
    }

    public void ResumeBGM(string expectedScene)
    {
        SceneBGMData data = GetSceneData(expectedScene);

        if (data == null)
            return;

        bool allowResume =
            (lastBGMType == BGMType.Normal && data.bgmType == BGMType.Boss);

        if (!allowResume)
            return;

        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
    }


    public void ResetBGMState()
    {
        currentStage = -1;
        currentType = BGMType.Main;
        lastBGMType = BGMType.Main;
        lastSceneName = "";

        shopOpenPlayed = false;

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
        shopOpenPlayed = false;
    }
    /*public void SetCutsceneState(bool playing)
    {
        isCutscenePlaying = playing;

        if (playing)
        {
            if (bossBGMCoroutine != null)
            {
                StopCoroutine(bossBGMCoroutine); // 중요: 딜레이 중이면 끊기
            }

            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
        }
    }*/
    public void SetCutsceneState(bool playing)
    {
        isCutscenePlaying = playing;

        if (playing)
        {
            if (bossBGMCoroutine != null)
                StopCoroutine(bossBGMCoroutine);

            if (shopCoroutine != null)
                StopCoroutine(shopCoroutine);

            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
        }
    }

    private IEnumerator PlayShopOpenDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isCutscenePlaying && shopOpenSound != null)
        {
            PlayClip(shopOpenSound);
        }
    }
    private IEnumerator PlayShopOpenOnce(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isCutscenePlaying && shopOpenSound != null)
        {
            bgmSource.PlayOneShot(shopOpenSound);
        }
    }
    private void StopShopIfNeeded(BGMType newType)
    {
        if (currentType == BGMType.Shop && newType != BGMType.Shop)
        {
            bgmSource.Stop();
            bgmSource.loop = false;
            bgmSource.clip = null;
        }
    }
}