using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 게임 전체 UI의 최상위 매니저
/// - Main 씬 버튼 처리
/// - 씬 전환 시 UI 다시 찾기
/// - PlayerUIManager / AugUIManager 바인딩
/// - ESC 입력 분기
/// - 새 게임 시작 / 재시작 / 메인 복귀 처리
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }


    [Header("UI캔버스")] [SerializeField] private GameObject rootUI;
    [SerializeField] private GameObject systemUI;

    // Main Scene UI
    [Header("메인UI")] [SerializeField] private GameObject mainImage;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject exitPanel;

    [SerializeField] private Button startBtn;
    [SerializeField] private Button optionBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button optionCloseBtn;
    [SerializeField] private Button exitYesBtn;
    [SerializeField] private Button exitNoBtn;
    [SerializeField] private Button exitCloseBtn;


    [Header("상점 UI")]
    //[SerializeField] private 
    // 하위 매니저
    [SerializeField]
    private PlayerUIManager playerUIManager;

    [SerializeField] private AugUIManager augUIManager;
    [SerializeField] private NewItemUIManager shopUIManager;
    [SerializeField] private ItemUIManager itemUIManager;

    [SerializeField] private OptionSettings optionSettings;
    [SerializeField] private SkillSelectUIManager skillUIManager;
    [SerializeField] private GameObject selectSkillPanelOverlay;
    public NewItemUIManager ShopUIManager => shopUIManager;

    // 다음 게임 씬에 들어갔을 때 플레이어 상태를 초기화해야 하는지
    private bool needResetPlayerOnNextScene;

    [Header("CurStageText")] [SerializeField]
    private TMPro.TextMeshProUGUI curStage;

    private OptionUI optionUI;
    [Header("게임 시작 컷씬")]
    [SerializeField] private bool playGameStartCutscene = true;

    private bool isStartingGame;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 시작하자마자 현재 씬 UI 바인딩
        BindSceneUI();
        optionUI = FindFirstObjectByType<OptionUI>();
        OptionUI.ApplySettingsStatic(optionSettings);
        // 현재 씬 이름에 맞는 초기 UI 상태 적용
        ApplySceneDefaultState(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 씬 로드 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /*private void Update()
    {
        // 키보드가 없으면 종료
        if (Keyboard.current == null) return;

        // ESC 입력이 이번 프레임에 눌렸는지 확인
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (optionUI != null && optionUI.HandleEscapeConsumed())
        {
            return;
        }

        // 메인 씬인지, 게임 씬인지에 따라 ESC 동작 분기
        if (SceneManager.GetActiveScene().name == "Main")
        {
            HandleMainSceneEscape();
        }
        else
        {
            HandleGameSceneEscape();
        }
    }*/
    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;


        //  매번 최신 OptionUI 가져오기
        optionUI = FindFirstObjectByType<OptionUI>();

        // 1순위: ControlViewPanel 닫기
        if (optionUI != null && optionUI.IsControlViewOpen)
        {
            optionUI.CloseControlView();
            return;
        }

        // 2순위: OptionPanel 닫기
        if (optionPanel != null && optionPanel.activeSelf)
        {
            optionPanel.SetActive(false);
            return;
        }

        // 3순위
        if (SceneManager.GetActiveScene().name == "Main")
            HandleMainSceneEscape();
        else
            HandleGameSceneEscape();
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출됨
    /// 여기서 현재 씬 UI를 다시 찾아서 바인딩함
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 이전 씬에서 TimeScale 멈춘 상태 방지
        Time.timeScale = 1f;
        StartCoroutine(RebindLate());
        // UI 바인딩은 한 프레임 뒤에 (중요: 오브젝트 생성 타이밍 문제 방지)
        StartCoroutine(DelayedBind(scene.name));
        optionUI = FindFirstObjectByType<OptionUI>();

        if (playerUIManager != null)
        {
            if (PlayerController.Instance != null)
            {
                playerUIManager.BindPlayer(PlayerController.Instance);
                playerUIManager.BindPlayerUI(systemUI);
            }
        }

        if (needResetPlayerOnNextScene)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ResetPlayerForRestart();
            }

            needResetPlayerOnNextScene = false;
        }

        if (PlayerController.Instance != null)
        {
            BindCameraToPlayer(PlayerController.Instance);
        }

        UpdateStageUI(); //*
    }

    private IEnumerator RebindLate()
    {
        yield return null;
        yield return null;

        BindSceneUI();

        if (itemUIManager != null && systemUI != null)
            itemUIManager.BindItemUI(systemUI);
    }

    private IEnumerator DelayedBind(string sceneName)
    {
        yield return null;
        yield return null;

        BindSceneUI();
        ApplySceneDefaultState(sceneName);
        UpdateStageUI(); //*
    }

    /// <summary>
    /// 현재 씬의 RootUI / System_UI / 하위 UIManager들을 찾고 바인딩
    /// </summary>
    private void BindSceneUI()
    {
        // RootUI 찾기
        rootUI = GameObject.Find("RootUI");

        if (rootUI == null)
        {
            systemUI = null;
            playerUIManager = null;
            augUIManager = null;
            shopUIManager = null;
            return;
        }

        Transform systemTransform = FindChildRecursive(rootUI.transform, "System_UI");
        systemUI = systemTransform != null ? systemTransform.gameObject : null;

        playerUIManager = rootUI.GetComponentInChildren<PlayerUIManager>(true);
        augUIManager = rootUI.GetComponentInChildren<AugUIManager>(true);
        shopUIManager = rootUI.GetComponentInChildren<NewItemUIManager>(true);
        itemUIManager = rootUI.GetComponentInChildren<ItemUIManager>(true);
        skillUIManager = rootUI.GetComponentInChildren<SkillSelectUIManager>(true);


        if (playerUIManager != null && systemUI != null)
            playerUIManager.BindPlayerUI(systemUI);

        if (augUIManager != null && systemUI != null)
            augUIManager.BindAugUI(systemUI);

        if (shopUIManager != null && systemUI != null)
            shopUIManager.BindShopUI(systemUI);

        if (itemUIManager != null && systemUI != null)
            itemUIManager.BindItemUI(systemUI);


        /*if (inGameShopUIManager != null && systemUI != null)
            inGameShopUIManager.BindShopUI(systemUI);

        if (inventoryManager != null && systemUI != null)
            inventoryManager.BindInventoryUI(systemUI);*/

        if (skillUIManager != null && systemUI != null)
        {
            selectSkillPanelOverlay = FindChildRecursive(rootUI.transform, "SelectSkillPanelOverlay")?.gameObject;

            skillUIManager.BindSkillUI(systemUI, selectSkillPanelOverlay);
        }

        SkillSlotButton[] slots = FindObjectsByType<SkillSlotButton>(FindObjectsSortMode.None);

        foreach (var slot in slots)
        {
            slot.UpdateIcon();
        }


        BindMainSceneUI();
    }

    private IEnumerator BindPlayerAfterSceneLoad()
    {
        // PlayerController.Instance가 준비될 때까지 대기
        while (PlayerController.Instance == null)
            yield return null;

        // PlayerUIManager가 아직 없다면 더 진행할 수 없음
        if (playerUIManager == null)
            yield break;

        // 핵심:
        // 다음 씬의 PlayerController를
        // PlayerUIManager의 public PlayerController playerController에 넣어준다.
        playerUIManager.BindPlayer(PlayerController.Instance);
        BindCameraToPlayer(PlayerController.Instance);
    }

    /// <summary>
    /// Main 씬에서 사용하는 버튼 / 패널 찾기
    /// </summary>
    private void BindMainSceneUI()
    {
        if (rootUI == null) return;

        // 메인 UI 오브젝트 찾기
        mainImage = FindChildRecursive(rootUI.transform, "MainImage")?.gameObject;
        mainPanel = FindChildRecursive(rootUI.transform, "MainPanel")?.gameObject;
        optionPanel = FindChildRecursive(rootUI.transform, "OptionPanel")?.gameObject;
        exitPanel = FindChildRecursive(rootUI.transform, "ExitPanel")?.gameObject;

        // MainPanel 안에서 버튼 찾기
        Transform mainPanelTransform = FindChildRecursive(rootUI.transform, "MainPanel");
        if (mainPanelTransform != null)
        {
            startBtn = FindChildRecursive(mainPanelTransform, "StartBtn")?.GetComponent<Button>();
            optionBtn = FindChildRecursive(mainPanelTransform, "OptionBtn")?.GetComponent<Button>();
            exitBtn = FindChildRecursive(mainPanelTransform, "ExitBtn")?.GetComponent<Button>();
        }
        else
        {
            startBtn = null;
            optionBtn = null;
            exitBtn = null;
        }

        // 옵션 패널 닫기 버튼 찾기
        if (optionPanel != null)
        {
            optionCloseBtn = FindChildRecursive(optionPanel.transform, "OptionCloseBtn")?.GetComponent<Button>();
        }
        else
        {
            optionCloseBtn = null;
        }

        // 종료 패널 버튼 찾기
        if (exitPanel != null)
        {
            exitYesBtn = FindChildRecursive(exitPanel.transform, "ExitYesBtn")?.GetComponent<Button>();
            exitNoBtn = FindChildRecursive(exitPanel.transform, "ExitNoBtn")?.GetComponent<Button>();
            exitCloseBtn = FindChildRecursive(exitPanel.transform, "ExitCloseBtn")?.GetComponent<Button>();
        }
        else
        {
            exitYesBtn = null;
            exitNoBtn = null;
            exitCloseBtn = null;
        }

        // 버튼 리스너 재연결
        BindButton(startBtn, StartGame);
        BindButton(optionBtn, OpenMainOptionPanel);
        BindButton(exitBtn, OpenMainExitPanel);

        BindButton(optionCloseBtn, CloseMainOptionPanel);

        BindButton(exitYesBtn, ExitGame);
        BindButton(exitNoBtn, CloseMainExitPanel);
        BindButton(exitCloseBtn, CloseMainExitPanel);
    }

    /// <summary>
    /// 씬 이름에 따라 기본 켜짐 / 꺼짐 상태를 정리
    /// </summary>
    private void ApplySceneDefaultState(string sceneName)
    {
        if (sceneName == "Main")
        {
            isStartingGame = false;

            if (mainImage != null) mainImage.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);
            if (optionPanel != null) optionPanel.SetActive(false);
            if (exitPanel != null) exitPanel.SetActive(false);

            if (augUIManager != null)
            {
                augUIManager.ResetUIStateForRestart();
            }

            return;
        }

        if (mainImage != null) mainImage.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);

        if (optionPanel != null && SceneManager.GetActiveScene().name != "Main")
        {
            optionPanel.SetActive(false);
        }

        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
    }

    #region ESC Handling

    /// <summary>
    /// 메인 씬 ESC 처리
    /// - 옵션 열려 있으면 닫음
    /// - 종료 패널 열려 있으면 닫음
    /// - 둘 다 없으면 종료 패널 열기
    /// </summary>
    private void HandleMainSceneEscape()
    {
        if (optionPanel != null && optionPanel.activeSelf)
        {
            optionPanel.SetActive(false);
            return;
        }

        if (exitPanel != null && exitPanel.activeSelf)
        {
            exitPanel.SetActive(false);
            return;
        }

        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 게임 씬 ESC 처리
    /// 1. 증강 UI 열려 있으면 -> 증강창 숨기고 ESC 패널 오픈
    /// 2. 아니면 PlayerUIManager 쪽 ESC 처리
    /// </summary>
    private void HandleGameSceneEscape()
    {
        OptionUI optionUI = FindFirstObjectByType<OptionUI>();

        if (optionUI != null && optionUI.HandleEscapeConsumed())
        {
            return;
        }

        if (playerUIManager == null) return;

        // 증강창이 열려 있는 상태에서 ESC를 누르면
        // 증강창을 잠시 숨기고 ESC 패널을 띄움
        if (augUIManager != null && augUIManager.IsAugmentationVisible())
        {
            augUIManager.HideCurrentAugmentationUI();
            playerUIManager.OpenEscPanelFromAugmentation();
            return;
        }


        // 그 외는 PlayerUIManager에서 처리
        playerUIManager.HandleEscape();
    }

    #endregion

    #region Main Scene Button Methods

    public void StartGame()
    {
        if (isStartingGame)
            return;

        StartCoroutine(StartGameWithCutsceneRoutine());
    }
    private IEnumerator StartGameWithCutsceneRoutine()
    {
        isStartingGame = true;

        ResetRunSystems();

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.ClearPlayedCutscenes();
        }

        // 컷씬 매니저가 없거나 컷씬을 끈 경우 바로 게임 시작
        if (!playGameStartCutscene || VideoCutsceneManager.Instance == null)
        {
            StartNewRunAfterStartCutscene();
            yield break;
        }

        // 1. 먼저 검은 화면으로 덮는다.
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        // 2. 컷씬 패널을 켜둔다.
        VideoCutsceneManager.Instance.ShowCutscenePanel();
        VideoCutsceneManager.Instance.BringCutsceneToFront();

        // 3. TransitionPanel을 다시 최상단으로 올리고 검은 화면을 걷는다.
        //    그러면 컷씬이 자연스럽게 나타남.
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        // 4. 컷씬 재생.
        //    여기서는 컷씬이 끝나도 바로 패널을 끄지 않고 마지막 프레임을 유지시킨다.
        yield return VideoCutsceneManager.Instance.PlayCutsceneForExternalFade(
            VideoCutsceneType.GameStart,
            0,
            true
        );

        // 5. 컷씬이 끝났으면 다시 검은 화면으로 덮는다.
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        // 6. 검은 화면 뒤에서 컷씬 패널 끄기.
        VideoCutsceneManager.Instance.EndExternalFadeCutscene();

        // 7. 바로 맵 이동.
        StartNewRunAfterStartCutscene();
    }
    private void StartNewRunAfterStartCutscene()
    {
        Time.timeScale = 1f;

        if (MapFlowManager.Instance != null)
        {
            MapFlowManager.Instance.StartNewRun();
            return;
        }

        isStartingGame = false;
        Debug.LogError("[UIManager] MapFlowManager가 없음. Main 씬의 GameManager에 MapFlowManager를 붙여야 함.");
    }

    /// <summary>
    /// 현재 게임 씬 재시작
    /// </summary>
    public void RestartCurrentScene()
    {
        ResetRunSystems();

        if (MapFlowManager.Instance != null)
        {
            MapFlowManager.Instance.StartNewRun();
            return;
        }

        Debug.LogError("[UIManager] MapFlowManager가 없음. 재시작 불가.");
    }

    /// <summary>
    /// 메인 씬으로 돌아가기
    /// </summary>
    public void GoToMainScene()
    {
        ResetRunSystems();

        if (MapFlowManager.Instance != null)
        {
            MapFlowManager.Instance.ResetFlowStateOnly();
        }

        if (PlayerController.Instance != null)
        {
            Transform crosshair = PlayerController.Instance.GetCrosshairTransform();

            if (crosshair != null)
            {
                Destroy(crosshair.gameObject);
            }

            Destroy(PlayerController.Instance.gameObject);
        }

        SkillBagInteractable current = FindAnyObjectByType<SkillBagInteractable>();

        if (current != null)
        {
            typeof(SkillBagInteractable)
                .GetField("currentTarget",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(null, null);
        }

        SceneManager.LoadScene("Main");

        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 메인 씬 옵션 패널 열기
    /// </summary>
    private void OpenMainOptionPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(true);
    }

    /// <summary>
    /// 메인 씬 옵션 패널 닫기
    /// </summary>
    private void CloseMainOptionPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);
    }

    /// <summary>
    /// 메인 씬 종료 패널 열기
    /// </summary>
    private void OpenMainExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(true);
    }

    /// <summary>
    /// 메인 씬 종료 패널 닫기
    /// </summary>
    private void CloseMainExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(false);
    }

    #endregion

    /// <summary>
    /// 게임 종료
    /// </summary>
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 버튼에 이벤트 연결하기 전에 기존 리스너 전부 제거
    /// 씬 재로딩 시 중복 연결 방지용
    /// </summary>
    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 현재 씬의 Main Camera가 새 플레이어를 다시 따라가게 연결
    /// 직접 만든 CameraFollow 스크립트 기준
    /// </summary>
    public void BindCameraToPlayer(PlayerController player)
    {
        if (player == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        FollowCam follow = mainCam.GetComponent<FollowCam>();
        if (follow == null) return;

        follow.SetTarget(player.transform);
    }

    /// <summary>
    /// 이름으로 자식 오브젝트 재귀 탐색
    /// RootUI 아래 구조가 깊어도 찾을 수 있게 함
    /// </summary>
    public static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ResetRunSystems()
    {
        Time.timeScale = 1f;

        // 증강 보유 상태 초기화
        if (AugmentRunManager.Instance != null)
        {
            AugmentRunManager.Instance.ResetRun();
        }

        // 증강 UI 초기화
        if (augUIManager != null)
        {
            augUIManager.ResetUIStateForRestart();
        }

        // 아이템 UI 초기화
        if (CurItemUI.Instance != null)
        {
            CurItemUI.Instance.SetItems(new List<ItemData>());
        }

        // 스킬 초기화
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ResetSkills();
        }

        // StageClear 진행 상태 초기화
        if (StageClear.Instance != null)
        {
            StageClear.Instance.ResetRunStateOnly();
        }

        needResetPlayerOnNextScene = true;
    }

    public NewItemUIManager GetShopUI()
    {
        return shopUIManager;
    }

    public ItemUIManager GetItemUI()
    {
        return itemUIManager;
    }

    public ItemUIManager GetWeaponUI()
    {
        return itemUIManager;
    }

    private void UpdateStageUI() //*
    {
        if (curStage == null) return;
        if (StageClear.Instance == null) return;

        int stage = StageClear.Instance.GetCurrentStageNumber();

        curStage.text = stage.ToString();
    }
}