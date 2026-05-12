using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;


public class MainUI : MonoBehaviour
{
    
    public static MainUI Instance { get; private set; }
    
    [Header("Image")]
    [SerializeField] public GameObject mainImage;
    
    [Header("Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject exitPanel;

    [Header("MainBtn")]
    [SerializeField] private Button startBtb;
    [SerializeField] private Button optionBtn;
    [SerializeField] private Button exitBtn;

    [Header("Optionbtn")]
    [SerializeField] private Button optionColseBtn;

    [Header("ExitBtn")]
    [SerializeField] private Button exitYesBtn;
    [SerializeField] private Button exitNoBtn;
    [SerializeField] private Button exitCloseBtn;
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        
        //PlayerUIManager.Instance.playerTextUIPanel.SetActive(false);
    }

    private void Start()
    {
        mainPanel.SetActive(true);
        optionPanel.SetActive(false);
        exitPanel.SetActive(false);
        

        startBtb.onClick.AddListener(OnClickStart);
        optionBtn.onClick.AddListener(OnClickOption);
        exitBtn.onClick.AddListener(OnClickExit);

        optionColseBtn.onClick.AddListener(CloseOption);

        exitYesBtn.onClick.AddListener(OnClickExitGame);
        exitNoBtn.onClick.AddListener(CloseExit);
        exitCloseBtn.onClick.AddListener(CloseExit);
        //ShowMainCursor();
        
        // 메인 UI가 켜질 때 증강 UI는 강제로 꺼둠
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.ResetUIStateForRestart();
        }
    }
    private void OnEnable()
    {
        ShowMainCursor();
    }

    private void OnDisable()
    {
        HideMainCursor();
    }
    
    private void ShowMainCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    private void HideMainCursor()
    {
        Cursor.visible = false;
    }

    private void OnClickStart()
    {
        mainImage.SetActive(false);
        SceneManager.LoadScene("TestScene");
        
        PlayerUIManager.Instance.playerTextUIPanel.SetActive(true);
    }
    private void OnClickOption()
    {
        optionPanel.SetActive(true);
    }

    private void OnClickExit()
    {
        exitPanel.SetActive(true);
    }

    private void CloseOption()
    {
        optionPanel.SetActive(false);
    }

    private void CloseExit()
    {
        exitPanel.SetActive(false);
    }

    private void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}

