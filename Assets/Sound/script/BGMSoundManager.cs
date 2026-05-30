/*using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMSoundManager : MonoBehaviour
{
    public static BGMSoundManager Instance;
    private string lastSceneName;
    private bool blockNextResume = false;
    private BGMType lastBGMType;
    private bool isCutscenePlaying = false; //*

    private Coroutine bossBGMCoroutine;
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

        public float bossBGMDelay;
    }

    [System.Serializable]
    private class StageBGM
    {
        public int stageNumber;
        public AudioClip normalBGM;
        public AudioClip bossBGM;
    }

    [Header("���� ����")]
    [SerializeField] private SceneBGMData[] sceneDatas;

    [Header("���� BGM")]
    [SerializeField] private AudioClip mainBGM;

    [Header("�������� BGM")]
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

        DontDestroyOnLoad(this);
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
        //Debug.Log($"[BGM] OnSceneLoaded: {scene.name}");

        ApplyBGM(scene.name);
    }

    /*private void ApplyBGM(string sceneName)
    {
        lastSceneName = sceneName;
        SceneBGMData data = GetSceneData(sceneName);

        if (data == null)
        {
            Debug.LogError($"[BGM] �� ������ ���� �� {sceneName}");
            return;
        }

        // ����
        if (data.bgmType == BGMType.Main)
        {
            PlayClip(mainBGM);
            return;
        }

        StageBGM stageData = GetStageBGM(data.stageNumber);

        if (stageData == null)
        {
            Debug.LogError($"[BGM] �������� ������ ���� �� {data.stageNumber}");
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
            Debug.LogError("[BGM] Ŭ�� ����");
            return;
        }

        // ���� �������� Normal/Shop�̸� ����
        if (currentStage == data.stageNumber &&
            currentType != BGMType.Boss &&
            data.bgmType != BGMType.Boss)
        {
            Debug.Log("[BGM] ������");
            return;
        }

        lastBGMType = currentType;

        PlayClip(target);

        currentStage = data.stageNumber;
        currentType = data.bgmType;
        
    }

    private void ApplyBGM(string sceneName)
    {
        // 추가
        if (isCutscenePlaying)
        {
            Debug.Log("[BGM] 컷씬 중이라 BGM 재생 차단");
            return;
        }

        lastSceneName = sceneName;
        SceneBGMData data = GetSceneData(sceneName);

        if (data == null)
        {
            Debug.LogError($"[BGM] 씬 데이터 없음: {sceneName}");
            return;
        }

        if (data.bgmType == BGMType.Main)
        {
            PlayClip(mainBGM);
            return;
        }

        StageBGM stageData = GetStageBGM(data.stageNumber);

        if (stageData == null)
        {
            Debug.LogError($"[BGM] 스테이지 데이터 없음: {data.stageNumber}");
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

                currentStage = data.stageNumber;
                currentType = data.bgmType;
                return; // 여기서 끝내야 즉시 재생 안됨
        }

        if (target == null)
        {
            Debug.LogError("[BGM] 클립 없음");
            return;
        }

        if (currentStage == data.stageNumber &&
            currentType != BGMType.Boss &&
            data.bgmType != BGMType.Boss)
        {
            Debug.Log("[BGM] 유지");
            return;
        }

        lastBGMType = currentType;

        PlayClip(target);

        currentStage = data.stageNumber;
        currentType = data.bgmType;
    }

    private void PlayClip(AudioClip clip)
    {
        if (bgmSource.clip == clip)
        {
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
                Debug.Log($"[BGM] PlayClip {clip.name}");
            }
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
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
            Debug.Log("[BGM] �Ͻ�����");
        }
    }
    public void ForceApplyBGM(string sceneName)
    {
        Debug.Log("[BGM] ���� ������");
        ApplyBGM(sceneName);
    }
    public void ResumeBGM(string expectedScene)
    {
        // ���� �� ������ ��������
        SceneBGMData data = GetSceneData(expectedScene);

        if (data == null)
            return;

        bool allowResume =
            (lastBGMType == BGMType.Normal && data.bgmType == BGMType.Boss);

        if (!allowResume)
        {
            Debug.Log("[BGM] Resume ���� ������ �� ����");
            return;
        }

        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.UnPause();
            Debug.Log("[BGM] �簳");
        }
    }
    public void BlockNextResume()
    {
        blockNextResume = true;
    }

    public void ResetBGMState()
    {
        Debug.Log("[BGM] ResetBGMState");

        currentStage = -1;
        currentType = BGMType.Main;
        lastBGMType = BGMType.Main;
        lastSceneName = "";

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }
    public void SetCutsceneState(bool playing)
    {
        isCutscenePlaying = playing;

        if (playing)
        {
            Debug.Log("[BGM] 컷씬 시작 → BGM 강제 정지");

            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
        }
        else
        {
            Debug.Log("[BGM] 컷씬 종료 → BGM 재개 준비");
        }
    }
}*/

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

        if (data.bgmType == BGMType.Main)
        {
            PlayClip(mainBGM);
            return;
        }

        StageBGM stageData = GetStageBGM(data.stageNumber);

        if (stageData == null)
        {
            Debug.LogError($"[BGM] 스테이지 데이터 없음: {data.stageNumber}");
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

                if (bossBGMCoroutine != null)
                {
                    StopCoroutine(bossBGMCoroutine);
                }

                bossBGMCoroutine = StartCoroutine(
                    PlayBossBGMWithDelay(target, stageData.bossBGMDelay)
                );

                currentStage = data.stageNumber;
                currentType = data.bgmType;
                return;
        }

        if (target == null)
        {
            Debug.LogError("[BGM] 클립 없음");
            return;
        }

        if (currentStage == data.stageNumber &&
            currentType != BGMType.Boss &&
            data.bgmType != BGMType.Boss)
        {
            return;
        }

        lastBGMType = currentType;

        PlayClip(target);

        currentStage = data.stageNumber;
        currentType = data.bgmType;
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
            {
                bgmSource.Play();
            }
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

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    public void SetCutsceneState(bool playing)
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
    }
}