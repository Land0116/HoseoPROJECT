using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 UI 전담 매니저
/// - HP / Gold 표시
/// - ESC 패널
/// - 옵션 패널
/// - 메인 복귀 / 종료 확인 패널
/// - 사망 UI
/// 
/// 중요한 점:
/// - 씬 전환은 여기서 직접 하지 않고 UIManager에 요청만 함
/// - MainUI 같은 별도 클래스는 더 이상 사용하지 않음
/// </summary>
public class PlayerUIManager : MonoBehaviour
{
    public PlayerController playerController;
    public static PlayerUIManager Instance { get; private set; }

    [Header("PlayerTextUI")]
    [SerializeField] public GameObject playerTextUIPanel;
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
    [SerializeField] private Button escToOptionCloseBtn;

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

    [Header("Equipment UI")]
    [SerializeField] private Image qSlotIcon;
    [SerializeField] private Image eSlotIcon;

    [SerializeField] private EquipmentSlot qSlot;
    [SerializeField] private EquipmentSlot eSlot;

    [Header("Cooldown UI")]
    [SerializeField] private Image qCooldownOverlay;
    [SerializeField] private TextMeshProUGUI qCooldownText;

    [SerializeField] private Image eCooldownOverlay;
    [SerializeField] private TextMeshProUGUI eCooldownText;
    
    [SerializeField] private GameObject dashPanel;
    [SerializeField] private Image dashIcon;
    [SerializeField] private Image dashCooldownOverlay;
    [SerializeField] private TextMeshProUGUI dashCooldownText;

    /// <summary>
    /// ESC 패널을 닫았을 때 어디로 되돌아가야 하는지 기억하는 값
    /// None        : 그냥 게임으로 복귀
    /// Augmentation: 증강창으로 복귀
    /// Shop        : 상점창으로 복귀
    /// </summary>
    private enum EscReturnTarget
    {
        None,
        Augmentation,
        Shop
    }
    [SerializeField] private EscReturnTarget escReturnTarget = EscReturnTarget.None;
    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 패널 초기 상태 정리
        ResetPanels();
    }

    private void Update()
    {
        if (playerController == null) return;
        // HP / Gold UI 갱신
        UpdatePlayerStateUI();

        UpdateCooldownUI();
    }
    private void UpdateCooldownUI()
    {
        UpdateSlotCooldown(qSlot, qCooldownOverlay, qCooldownText);
        UpdateSlotCooldown(eSlot, eCooldownOverlay, eCooldownText);
        
        UpdateDashCooldownUI();
    }

    private void UpdateSlotCooldown(EquipmentSlot slot, Image overlay, TextMeshProUGUI text)
    {
        if (slot == null || slot.item == null)
        {
            overlay.gameObject.SetActive(false);
            text.text = "";
            return;
        }

        var item = slot.item;

        if (item.lastUseTime < 0)
        {
            overlay.gameObject.SetActive(false);
            text.text = "";
            return;
        }

        float remain = (item.lastUseTime + item.data.cooldown) - Time.time;

        if (remain > 0)
        {
            overlay.gameObject.SetActive(true);
            text.gameObject.SetActive(true);

            float ratio = remain / item.data.cooldown;
            overlay.fillAmount = ratio;

            text.text = Mathf.Ceil(remain).ToString();
        }
        else
        {
            overlay.fillAmount = 0;
            overlay.gameObject.SetActive(false);
            text.text = "";
        }
    }
    /// <summary>
    /// 플레이어 상태 UI 갱신
    /// </summary>
    private void UpdatePlayerStateUI()
    {
        if (playerController == null) return;
        if (hpBar == null || hpText == null || goldText == null) return;

        // HP가 0보다 아래로 내려가지 않게 보정
        if (playerController.Hp <= 0)
            playerController.Hp = 0;

        // HP 텍스트
        hpText.text = playerController.Hp + " / " + playerController.MaxHp;

        // HP 바 부드럽게 보간
        hpBar.value = Mathf.Lerp(
            hpBar.value,
            (float)playerController.Hp / playerController.MaxHp,
            Time.unscaledDeltaTime * 10f
        );

        // 죽은 상태면 Gold 업데이트는 안 해도 됨
        if (playerController.IsDie) return;

        // Gold 텍스트
        goldText.text = "G : " + playerController.Gold;
    }

    /// <summary>
    /// 현재 패널을 닫고 새 패널 열기
    /// </summary>
    private void OpenPanel(GameObject panel)
    {
        if (panel == null) return;

        if (currentPanel != null)
            currentPanel.SetActive(false);

        panel.SetActive(true);
        currentPanel = panel;
    }

    /// <summary>
    /// ESC 입력 처리
    /// 
    /// 동작 규칙:
    /// 1. 아무 패널도 없으면 -> ESC 패널 열기
    /// 2. 옵션 패널 열려 있으면 -> ESC 패널로 돌아가기
    /// 3. 종료/메인복귀 확인 패널 열려 있으면 -> ESC 패널로 돌아가기
    /// 4. ESC 패널 열려 있으면
    ///    - None        : 게임 복귀
    ///    - Augmentation: 증강창 복귀
    ///    - Shop        : 상점창 복귀
    /// </summary>
    public void HandleEscape()
    {
        if (currentPanel == null)
        {
            OpenEscPanelNormal();
            return;
        }

        if (currentPanel == optionPanel)
        {
            OpenPanel(escPanel);

            if (escToOptionCloseBtn != null)
                escToOptionCloseBtn.gameObject.SetActive(false);

            return;
        }

        if (currentPanel == exitSurePanel || currentPanel == toMainSurePanel)
        {
            OpenPanel(escPanel);
            return;
        }

        if (currentPanel == escPanel)
        {
            switch (escReturnTarget)
            {
                case EscReturnTarget.Augmentation:
                    CloseEscAndReturnToAugmentation();
                    break;

                case EscReturnTarget.Shop:
                    CloseEscAndReturnToShop();
                    break;

                default:
                    CloseEscAndResumeGameplay();
                    break;
            }

            return;
        }
    }

    /// <summary>
    /// 일반 상황에서 ESC 패널 열기
    /// </summary>
    private void OpenEscPanelNormal()
    {
        escReturnTarget = EscReturnTarget.None;

        OpenPanel(escPanel);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }
    
    /// <summary>
    /// 증강창에서 ESC를 눌러 넘어온 경우의 ESC 패널 열기
    /// </summary>
    public void OpenEscPanelFromAugmentation()
    {
        escReturnTarget = EscReturnTarget.Augmentation;

        OpenPanel(escPanel);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }
    
    /// <summary>
    /// 상점창에서 ESC를 눌러 넘어온 경우의 ESC 패널 열기
    /// </summary>
    public void OpenEscPanelFromShop()
    {
        escReturnTarget = EscReturnTarget.Shop;

        OpenPanel(escPanel);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }

    /// <summary>
    /// ESC 패널 닫고 게임 복귀
    /// </summary>
    private void CloseEscAndResumeGameplay()
    {
        if (escPanel != null)
            escPanel.SetActive(false);

        currentPanel = null;
        escReturnTarget = EscReturnTarget.None;

        Time.timeScale = 1f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(false);
    }

    /// <summary>
    /// ESC 패널 닫고 증강창으로 복귀
    /// </summary>
    private void CloseEscAndReturnToAugmentation()
    {
        if (escPanel != null)
            escPanel.SetActive(false);

        currentPanel = null;

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RestoreCurrentAugmentationUI();
        }

        // 증강창은 멈춘 상태 유지
        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }
    
    private void CloseEscAndReturnToShop()
    {
        if (escPanel != null)
            escPanel.SetActive(false);

        currentPanel = null;

        if (UIManager.Instance != null && UIManager.Instance.ShopUIManager != null)
        {
            UIManager.Instance.ShopUIManager.RestoreCurrentShopUI();
        }

        // 상점창도 멈춘 상태 유지
        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }
    
    
    private void UpdateDashCooldownUI()
    {
        if (playerController == null)
        {
            if (dashCooldownOverlay != null)
                dashCooldownOverlay.gameObject.SetActive(false);

            if (dashCooldownText != null)
                dashCooldownText.text = "";

            return;
        }

        float remain = playerController.GetDashCooldownRemain();
        float ratio = playerController.GetDashCooldownRatio();
        bool onCooldown = playerController.IsDashOnCooldown();

        // 쿨타임 중
        if (onCooldown)
        {
            if (dashCooldownOverlay != null)
            {
                dashCooldownOverlay.gameObject.SetActive(true);
                dashCooldownOverlay.fillAmount = ratio;
            }

            if (dashCooldownText != null)
            {
                dashCooldownText.gameObject.SetActive(true);
                dashCooldownText.text = Mathf.Max(0f, remain).ToString("F1");
            }

        }
        // 사용 가능
        else
        {
            if (dashCooldownOverlay != null)
            {
                dashCooldownOverlay.fillAmount = 0f;
                dashCooldownOverlay.gameObject.SetActive(false);
            }

            if (dashCooldownText != null)
            {
                dashCooldownText.text = "";
                dashCooldownText.gameObject.SetActive(true);
            }

            
        }
    }
    
    
    
    
    /// <summary>
    /// 옵션 패널 열기
    /// </summary>
    private void OpenOptionPanel()
    {
        OpenPanel(optionPanel);
        if (escToOptionCloseBtn != null)
            escToOptionCloseBtn.gameObject.SetActive(true);
    }

    /// <summary>
    /// 메인 복귀 확인 패널 열기
    /// </summary>
    private void OpenToMainSurePanel()
    {
        OpenPanel(toMainSurePanel);
    }

    /// <summary>
    /// 게임 종료 확인 패널 열기
    /// </summary>
    private void OpenExitSurePanel()
    {
        OpenPanel(exitSurePanel);
    }

    /// <summary>
    /// ESC 패널을 다시 열기
    /// 버튼에서 X 누르거나 No 누를 때 사용
    /// </summary>
    public void OpenEscPanel()
    {
        OpenPanel(escPanel);
    }

    /// <summary>
    /// 메인으로 돌아가기
    /// 실제 씬 로드는 UIManager가 담당
    /// </summary>
    private void OnClickToMain()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.GoToMainScene();
        }
    }

    /// <summary>
    /// 현재 씬 재시작
    /// 실제 씬 로드는 UIManager가 담당
    /// </summary>
    private void OnClickRestart()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartCurrentScene();
        }
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    private void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 사망 UI 보여주기
    /// </summary>
    public void ShowPlayerDyingUI()
    {
        OpenPanel(playerDyingPanel);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }
    
    /// <summary>
    /// 플레이어 HUD(HP, Gold 등 기본 UI)를 켠다.
    /// 게임 씬 진입 후 / 증강 선택 후 다시 보이게 할 때 사용.
    /// </summary>
    public void ShowPlayerHUD()
    {
        if (playerTextUIPanel != null)
            playerTextUIPanel.SetActive(true);
    }

    /// <summary>
    /// 플레이어 HUD를 끈다.
    /// 필요하면 메인 복귀나 특수 연출 때 사용.
    /// </summary>
    public void HidePlayerHUD()
    {
        if (playerTextUIPanel != null)
            playerTextUIPanel.SetActive(false);
    }

    /// <summary>
    /// 플레이어 재바인딩
    /// 씬이 새로 로드될 때 UIManager가 호출
    /// </summary>
    public void BindPlayer(PlayerController player)
    {
        playerController = player;

        if (playerDyingPanel != null)
            playerDyingPanel.SetActive(false);
        
        ResetDashUI();
    }

    /// <summary>
    /// System_UI 아래 오브젝트를 이름으로 찾아 연결
    /// </summary>
    public void BindPlayerUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        // PlayerUIPanel 찾기
        Transform playerPanel = UIManager.FindChildRecursive(systemUIRoot.transform, "PlayerUIPanel");
        if (playerPanel == null) return;

        // 플레이어 상태 UI
        playerTextUIPanel = playerPanel.gameObject;
        hpBar = UIManager.FindChildRecursive(playerPanel, "PlayerHpBar")?.GetComponent<Slider>();
        hpText = UIManager.FindChildRecursive(playerPanel, "HpTxt")?.GetComponent<TextMeshProUGUI>();
        goldText = UIManager.FindChildRecursive(playerPanel, "GoldTxt")?.GetComponent<TextMeshProUGUI>();
        // 패널들
        escPanel = UIManager.FindChildRecursive(playerPanel, "EscPanel")?.gameObject;

        // OptionPanel은 System_UI 아래 공용 패널을 사용
        optionPanel = UIManager.FindChildRecursive(systemUIRoot.transform, "OptionPanel")?.gameObject;

        exitSurePanel = UIManager.FindChildRecursive(playerPanel, "ExitSurePanel")?.gameObject;
        toMainSurePanel = UIManager.FindChildRecursive(playerPanel, "ToMainSurePanel")?.gameObject;
        playerDyingPanel = UIManager.FindChildRecursive(systemUIRoot.transform, "PlayerDyingPanel")?.gameObject;
        
        Transform dashPanelTr = UIManager.FindChildRecursive(playerPanel, "DashPanel");
        if (dashPanelTr != null)
        {
            dashPanel = dashPanelTr.gameObject;
            dashIcon = UIManager.FindChildRecursive(dashPanelTr, "DashIcon")?.GetComponent<Image>();
            dashCooldownOverlay = UIManager.FindChildRecursive(dashPanelTr, "DashCooldownOverlay")?.GetComponent<Image>();
            dashCooldownText = UIManager.FindChildRecursive(dashPanelTr, "DashCooldownText")?.GetComponent<TextMeshProUGUI>();


        }
        

        // ESC 패널 내부 버튼
        optionBtn = UIManager.FindChildRecursive(playerPanel, "OptionBtn")?.GetComponent<Button>();
        toMainBtn = UIManager.FindChildRecursive(playerPanel, "ToMainBtn")?.GetComponent<Button>();
        exitGameBtn = UIManager.FindChildRecursive(playerPanel, "ExitGameBtn")?.GetComponent<Button>();
        xBtn = UIManager.FindChildRecursive(systemUIRoot.transform, "OptionCloseBtn")?.GetComponent<Button>();
        escToOptionCloseBtn = UIManager.FindChildRecursive(systemUIRoot.transform, "EscToOptionCloseBtn")?.GetComponent<Button>(); //*
        // 종료 확인 패널 버튼
        if (exitSurePanel != null)
        {
            exitSureYesBtn = UIManager.FindChildRecursive(exitSurePanel.transform, "ExitSureYesBtn")?.GetComponent<Button>();
            exitSureNoBtn = UIManager.FindChildRecursive(exitSurePanel.transform, "ExitSureNoBtn")?.GetComponent<Button>();
            exitSureXBtn = UIManager.FindChildRecursive(exitSurePanel.transform, "ExitSureXBtn")?.GetComponent<Button>();
        }

        // 메인복귀 확인 패널 버튼
        if (toMainSurePanel != null)
        {
            toMainSureYesBtn = UIManager.FindChildRecursive(toMainSurePanel.transform, "ToMainSureYesBtn")?.GetComponent<Button>();
            toMainSureNoBtn = UIManager.FindChildRecursive(toMainSurePanel.transform, "ToMainSureNoBtn")?.GetComponent<Button>();
            toMainSureXBtn = UIManager.FindChildRecursive(toMainSurePanel.transform, "ToMainSureXBtn")?.GetComponent<Button>();
        }

        // 사망 UI 버튼
        if (playerDyingPanel != null)
        {
            reStart = UIManager.FindChildRecursive(playerDyingPanel.transform, "ReStartBtn")?.GetComponent<Button>();
            dieToMain = UIManager.FindChildRecursive(playerDyingPanel.transform, "DieToMainBtn")?.GetComponent<Button>();
        }

        // 버튼 이벤트 연결
        BindButtons();

        // 씬 들어올 때 패널 상태 초기화
        ResetPanels();
        
        ShowPlayerHUD();
    }

    /// <summary>
    /// 버튼 이벤트 연결
    /// 씬 재로드 시 중복 리스너 방지 위해 RemoveAllListeners 사용
    /// </summary>
    private void BindButtons()
    {
        BindButton(optionBtn, OpenOptionPanel);
        BindButton(toMainBtn, OpenToMainSurePanel);
        BindButton(exitGameBtn, OpenExitSurePanel);
        BindButton(xBtn, OpenEscPanel);
        BindButton(escToOptionCloseBtn, HandleEscape);//*

        BindButton(exitSureYesBtn, OnClickExitGame);
        BindButton(exitSureNoBtn, OpenEscPanel);
        BindButton(exitSureXBtn, OpenEscPanel);

        BindButton(toMainSureYesBtn, OnClickToMain);
        BindButton(toMainSureNoBtn, OpenEscPanel);
        BindButton(toMainSureXBtn, OpenEscPanel);

        BindButton(reStart, OnClickRestart);
        BindButton(dieToMain, OnClickToMain);
    }
    
    /// <summary>
    /// 모든 패널 초기 상태 정리
    /// </summary>
    private void ResetPanels()
    {
        if (escPanel != null) escPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (exitSurePanel != null) exitSurePanel.SetActive(false);
        if (toMainSurePanel != null) toMainSurePanel.SetActive(false);
        if (playerDyingPanel != null) playerDyingPanel.SetActive(false);

        currentPanel = null;
        escReturnTarget = EscReturnTarget.None;
    }
    
    private void ResetDashUI()
    {
        if (dashCooldownOverlay != null)
        {
            dashCooldownOverlay.fillAmount = 0f;
            dashCooldownOverlay.gameObject.SetActive(false);
        }

        if (dashCooldownText != null)
        {
            dashCooldownText.text = "";
            dashCooldownText.gameObject.SetActive(true);
        }

    }

    /// <summary>
    /// 버튼 바인딩 공통 함수
    /// </summary>
    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    //플레이어 상점 아이템 장착
    public void SetEquipment(EquipmentSlot.SlotType type, ItemInstance item)
    {
        switch (type)
        {
            case EquipmentSlot.SlotType.Q:
                if (item == null)
                {
                    qSlotIcon.sprite = null;
                    qSlotIcon.enabled = false;
                }
                else
                {
                    qSlotIcon.sprite = item.data.icon;
                    qSlotIcon.enabled = true;
                }
                break;

            case EquipmentSlot.SlotType.E:
                if (item == null)
                {
                    eSlotIcon.sprite = null;
                    eSlotIcon.enabled = false;
                }
                else
                {
                    eSlotIcon.sprite = item.data.icon;
                    eSlotIcon.enabled = true;
                }
                break;
        }
    }

    public void ResetEquipmentUI()
    {
        // Q 슬롯 초기화
        if (qSlotIcon != null)
        {
            qSlotIcon.sprite = null;
            qSlotIcon.enabled = false;
        }

        // E 슬롯 초기화
        if (eSlotIcon != null)
        {
            eSlotIcon.sprite = null;
            eSlotIcon.enabled = false;
        }

    }
    public void UseEquipment(EquipmentSlot.SlotType type) //*
    {
        switch (type)
        {
            case EquipmentSlot.SlotType.Q:
                if (qSlot != null) qSlot.TryUse();
                break;

            case EquipmentSlot.SlotType.E:
                if (eSlot != null) eSlot.TryUse();
                break;
        }
    }

}