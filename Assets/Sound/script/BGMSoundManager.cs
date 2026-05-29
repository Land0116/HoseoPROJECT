using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMSoundManager : MonoBehaviour
{
    public static BGMSoundManager Instance;
    private string lastSceneName;
    private bool blockNextResume = false;
    private BGMType lastBGMType;

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
        Debug.Log($"[BGM] �� �ε�: {scene.name}");

        ApplyBGM(scene.name);
    }

    private void ApplyBGM(string sceneName)
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

    /*private void PlayClip(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log($"[BGM] ��� �� {clip.name}");
    }*/
    private void PlayClip(AudioClip clip)
    {
        if (bgmSource.clip == clip)
        {
            // ���� Ŭ���ε� ���������� �ٽ� ���
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
                Debug.Log($"[BGM] ���� Ŭ�� ��� ���� �� {clip.name}");
            }
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log($"[BGM] ��� �� {clip.name}");
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
        Debug.Log("[BGM] ���� �ʱ�ȭ");

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
}