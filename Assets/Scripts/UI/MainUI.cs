using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;


public class MainUI : MonoBehaviour
{
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
    }

    private void OnClickStart()
    {
        SceneManager.LoadScene("TestScene");
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
