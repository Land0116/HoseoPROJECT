using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerUIManager : MonoBehaviour
{
    public PlayerController playerController;
    public static PlayerUIManager instance;
    [SerializeField] private string restartSceneName = "TestScene";
    
    [Header("플레이어 정보 및 UI")]
    [SerializeField] private GameObject playerTextUIPanel;

    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [Header("플레이어 DeathUI")]
    [SerializeField] private GameObject playerDeathUIPanel;
    
    
    [Header("증강시스템UI")]
    [SerializeField] private GameObject augmentationUIPanel;
    

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        BindPlayer(PlayerController.Instance);
    }
    void Update()
    {
        PlayerStateUI();
    }
    

    private void PlayerStateUI()
    {
        if (playerController == null || hpBar == null || hpText == null || goldText == null)
            return;

        if (playerController.Hp <= 0)
        {
            playerController.Hp = 0;
        }

        hpText.text = playerController.Hp.ToString() + " / " + playerController.MaxHp.ToString();
        hpBar.value = Mathf.Lerp(hpBar.value,
            (float)playerController.Hp / (float)playerController.MaxHp,
            Time.deltaTime);

        if (playerController.IsDie)
            return;

        goldText.text = "G : " + playerController.Gold.ToString();
        
    }


    public void ShowPlayerDeathUI()
    {
        if (playerController != null && playerController.IsDie)
        {
            playerDeathUIPanel.SetActive(true);
        }
    }
    
    public void GameReStart()
    {
        Time.timeScale = 1f;

        // 이번 런 증강 초기화
        if (AugmentRunManager.Instance != null)
        {
            AugmentRunManager.Instance.ResetRun();
        }

        // UI 상태 초기화 + 다음 씬에서 증강창 다시 띄우기 예약
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.ResetUIStateForRestart();
            AugUIManager.instance.RequestShowOnNextScene();
        }
        
        

        // 플레이어 상태 초기화
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ResetPlayerForRestart();
        }

        // 첫 씬으로 돌아가거나 원하는 씬으로 이동
        SceneManager.LoadScene(restartSceneName);
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
        GameObject systemUI = GameObject.Find("System_UI");

        if (PlayerUIManager.instance != null)
        {
            PlayerUIManager.instance.BindPlayerUI(systemUI);
        }

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.BindAugUI(systemUI);
        }
    }
    
    public void BindPlayer(PlayerController player)
    {
        playerController = player;

        if (playerDeathUIPanel != null)
        {
            playerDeathUIPanel.SetActive(false);
        }
    }
    public void BindPlayerUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform playerPanel = systemUIRoot.transform.Find("PlayerUIPanel");
        Transform augPanel = systemUIRoot.transform.Find("AugmentationUIPanel");
        Transform deathPanel = systemUIRoot.transform.Find("PlayerDeathUI");

        if (playerPanel != null)
        {
            playerTextUIPanel = playerPanel.gameObject;

            if (hpBar == null)
                hpBar = playerPanel.GetComponentInChildren<Slider>(true);

            TextMeshProUGUI[] texts = playerPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name.Contains("Hp") || t.name.Contains("HP"))
                    hpText = t;

                if (t.name.Contains("Gold"))
                    goldText = t;
            }
        }

        if (augPanel != null)
            augmentationUIPanel = augPanel.gameObject;

        if (deathPanel != null)
            playerDeathUIPanel = deathPanel.gameObject;
    }
}
