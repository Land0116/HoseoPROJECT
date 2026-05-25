using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMSoundManager : MonoBehaviour
{
    public static BGMSoundManager Instance;
    private string lastSceneName;
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
    }

    [Header("씬별 설정")]
    [SerializeField] private SceneBGMData[] sceneDatas;

    [Header("메인 BGM")]
    [SerializeField] private AudioClip mainBGM;

    [Header("스테이지 BGM")]
    [SerializeField] private StageBGM[] stageBGMs;

    [SerializeField] private AudioSource bgmSource;

    private string currentSceneName = "";
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
        DontDestroyOnLoad(gameObject);
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
        Debug.Log($"[BGM] 씬 로드: {scene.name}");

        ApplyBGM(scene.name);
    }

    private void ApplyBGM(string sceneName)
    {
        lastSceneName = sceneName;
        SceneBGMData data = GetSceneData(sceneName);

        if (data == null)
        {
            Debug.LogError($"[BGM] 씬 데이터 없음 → {sceneName}");
            return;
        }

        // 메인
        if (data.bgmType == BGMType.Main)
        {
            PlayClip(mainBGM);
            return;
        }

        StageBGM stageData = GetStageBGM(data.stageNumber);

        if (stageData == null)
        {
            Debug.LogError($"[BGM] 스테이지 데이터 없음 → {data.stageNumber}");
            return;
        }

        AudioClip target = null;

        switch (data.bgmType)
        {
            case BGMType.Normal:
            case BGMType.Shop:
                target = stageData.normalBGM;
                break;

            case BGMType.Boss:
                target = stageData.bossBGM;
                break;
        }

        if (target == null)
        {
            Debug.LogError("[BGM] 클립 없음");
            return;
        }

        // 같은 스테이지 Normal/Shop이면 유지
        if (currentStage == data.stageNumber &&
            currentType != BGMType.Boss &&
            data.bgmType != BGMType.Boss)
        {
            Debug.Log("[BGM] 유지됨");
            return;
        }

        PlayClip(target);

        currentStage = data.stageNumber;
        currentType = data.bgmType;
    }

    private void PlayClip(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log($"[BGM] 재생 → {clip.name}");
    }

    private SceneBGMData GetSceneData(string sceneName)
    {
        for (int i = 0; i < sceneDatas.Length; i++)
        {
            if (sceneDatas[i].sceneName == sceneName)
                return sceneDatas[i];
        }

        return null;
    }

    private StageBGM GetStageBGM(int stage)
    {
        for (int i = 0; i < stageBGMs.Length; i++)
        {
            if (stageBGMs[i].stageNumber == stage)
                return stageBGMs[i];
        }

        return null;
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
            Debug.Log("[BGM] 일시정지");
        }
    }

    public void ResumeBGM(string expectedScene)
    {
        if (lastSceneName != expectedScene)
        {
            Debug.Log("[BGM] 씬 바뀜 → Resume 취소");
            return;
        }

        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.UnPause();
            Debug.Log("[BGM] 재개");
        }
    }
}