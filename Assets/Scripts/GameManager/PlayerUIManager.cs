using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerUIManager : MonoBehaviour
{
    public PlayerController playerController;
    public static PlayerUIManager Instance { get; private set; }

    [Header("PlayerTextUI")]
    [SerializeField] private GameObject playerTextUIPanel;
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("AugmentationUI")]
    [SerializeField] private GameObject augmentationUIPanel;

    [Header("Pause")]
    [SerializeField] private GameObject escPanel;
    [SerializeField] private GameObject optionPanel;

    [SerializeField] private Button optionBtn;
    [SerializeField] private Button toMainBtn;
    [SerializeField] private Button exitGameBtn;
    [SerializeField] private Button xBtn;

    [Header("ExitSure")]
    [SerializeField] private GameObject exitSurePanel;
    [SerializeField] private Button exitSureYesBtn;
    [SerializeField] private Button exitSureNoBtn;
    [SerializeField] private Button exitSureXBtn;

    [Header("ToMainSure")]
    [SerializeField] private GameObject toMainSurePanel;
    [SerializeField] private Button toMainSureYesBtn;
    [SerializeField] private Button toMainSureNoBtn;
    [SerializeField] private Button toMainSureXBtn;
    private GameObject currentPanel;

    [Header("PlayerDying")]
    [SerializeField] private GameObject playerDyingPanel;
    [SerializeField] private Button reStart;
    [SerializeField] private Button dieToMain;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        escPanel.SetActive(false);
        optionPanel.SetActive(false);
        exitSurePanel.SetActive(false);
        toMainSurePanel.SetActive(false);
        playerDyingPanel.SetActive(false);
        currentPanel = null;

        optionBtn.onClick.AddListener(OptionPanelTrue);
        toMainBtn.onClick.AddListener(ToMainSurePanel);
        exitGameBtn.onClick.AddListener(ExitSureTrue);
        xBtn.onClick.AddListener(EscPanelTrue);

        exitSureYesBtn.onClick.AddListener(OnClickExitGame);
        exitSureNoBtn.onClick.AddListener(EscPanelTrue);
        exitSureXBtn.onClick.AddListener(EscPanelTrue);

        toMainSureYesBtn.onClick.AddListener(OnClickToMain);
        toMainSureNoBtn.onClick.AddListener(EscPanelTrue);
        toMainSureXBtn.onClick.AddListener(EscPanelTrue);

        reStart.onClick.AddListener(OnClickRestart);
        dieToMain.onClick.AddListener(OnClickToMain);
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            return;
        }

        PlayerStateUI();
    }

    private void PlayerStateUI()
    {
        if (playerController == null) return;

        if (playerController.Hp <= 0)
            playerController.Hp = 0;

        hpText.text = playerController.Hp + " / " + playerController.MaxHp;
        hpBar.value = Mathf.Lerp(hpBar.value,
            (float)playerController.Hp / playerController.MaxHp,
            Time.deltaTime);

        if (playerController.IsDie) return;

        goldText.text = "G : " + playerController.Gold;
    }

    
    private void OpenPanel(GameObject panel)
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        panel.SetActive(true);
        currentPanel = panel;
    }
    
    public void HandleEscape()
    {
        
        if (currentPanel == null)
        {
            OpenPanel(escPanel);
            Time.timeScale = 0f;
            PlayerController.Instance.SetPause(true);
            return;
        }

       
        if (currentPanel == escPanel)
        {
            currentPanel.SetActive(false);
            currentPanel = null;

            Time.timeScale = 1f;
            PlayerController.Instance.SetPause(false);
            return;
        }

        OpenPanel(escPanel);
    }
    private void ToMainSurePanel()
    {
        OpenPanel(toMainSurePanel);
    }

    private void OptionPanelTrue()
    {
        OpenPanel(optionPanel);
    }

    public void EscPanelTrue()
    {
        OpenPanel(escPanel);
    }

    private void ExitSureTrue()
    {
        OpenPanel(exitSurePanel);
    }

    private void OnClickToMain()
    {
        SceneManager.LoadScene("Main");
    }

    private void OnClickRestart()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowPlayerDyingUI()
    {
        OpenPanel(playerDyingPanel);
    }
    private void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public void BindPlayer(PlayerController player)
    {
        playerController = player;

        if (playerDyingPanel != null)
        {
            playerDyingPanel.SetActive(false);
        }
    }
    

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        playerController = FindFirstObjectByType<PlayerController>();

        hpBar = FindFirstObjectByType<Slider>();
        hpText = FindFirstObjectByType<TextMeshProUGUI>();
        goldText = FindFirstObjectByType<TextMeshProUGUI>();

        currentPanel = null;
    }
}