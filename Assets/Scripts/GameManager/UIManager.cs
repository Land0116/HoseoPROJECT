using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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


    [Header("UI캔버스")]
    [SerializeField]private GameObject rootUI;
    [SerializeField]private GameObject systemUI;

    // Main Scene UI
    [Header("메인UI")]
    [SerializeField]private GameObject mainImage;
    [SerializeField]private GameObject mainPanel;
    [SerializeField]private GameObject optionPanel;
    [SerializeField]private GameObject exitPanel;

    [SerializeField]private Button startBtn;
    [SerializeField]private Button optionBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField]private Button optionCloseBtn;
    [SerializeField]private Button exitYesBtn;
    [SerializeField]private Button exitNoBtn;
    [SerializeField]private Button exitCloseBtn;


    [Header("상점 UI")]
    //[SerializeField] private 
    // 하위 매니저
    [SerializeField] private PlayerUIManager playerUIManager;
    [SerializeField] private AugUIManager augUIManager;
    [SerializeField] private ShopUIManager shopUIManager;
    [SerializeField] private ItemUIManager itemUIManager;
    [SerializeField] private InGameShopUIManager inGameShopUIManager;
    [SerializeField] private InventoryManager inventoryManager;


    [SerializeField] private OptionSettings optionSettings;
    public InventoryManager InventoryManager => inventoryManager;
    public ShopUIManager ShopUIManager => shopUIManager;

    // 다음 게임 씬에 들어갔을 때 플레이어 상태를 초기화해야 하는지
    private bool needResetPlayerOnNextScene;
    // 다음 게임 씬에 들어갔을 때 증강창을 바로 보여줘야 하는지
    private bool needShowAugmentationOnNextScene;

     private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 바뀌어도 유지
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 시작하자마자 현재 씬 UI 바인딩
        BindSceneUI();

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

    private void Update()
    {
        // 키보드가 없으면 종료
        if (Keyboard.current == null) return;

        // ESC 입력이 이번 프레임에 눌렸는지 확인
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // 메인 씬인지, 게임 씬인지에 따라 ESC 동작 분기
        if (SceneManager.GetActiveScene().name == "Main")
        {
            HandleMainSceneEscape();
        }
        else
        {
            HandleGameSceneEscape();
        }
    }

    /// <summary>
    /// 씬이 로드될 때마다 호출됨
    /// 여기서 현재 씬 UI를 다시 찾아서 바인딩함
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        


        // 혹시 이전 씬에서 timeScale이 0으로 멈춰 있었으면 복구
        Time.timeScale = 1f;

        // 현재 씬의 UI 다시 찾기
        StartCoroutine(DelayedBind(scene.name));

        // Main 씬이면 여기까지
        //if (scene.name == "Main")
        //    return;
        if (scene.name != "Main")
        {
            EquipmentSlot[] slots = FindObjectsByType<EquipmentSlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var slot in slots)
            {
                slot.ResetSlot();
            }
        }


        // 게임 씬에서는 PlayerController를 씬 로드 후에 다시 연결
        if (playerUIManager != null && PlayerController.Instance != null)
        {
            playerUIManager.BindPlayer(PlayerController.Instance);
        }

        // "새 게임 시작" 또는 "재시작" 예약이 있었다면
        if (needResetPlayerOnNextScene)
        {
            if (PlayerController.Instance != null)
            {
                // 플레이어 상태 초기화
                PlayerController.Instance.ResetPlayerForRestart();
            }

            needResetPlayerOnNextScene = false;
        }

        // 다음 씬에서 증강창을 띄우기로 예약되어 있으면
        if (needShowAugmentationOnNextScene)
        {
            if (augUIManager != null)
            {
                augUIManager.RequestShowOnNextScene();
                augUIManager.TryOpenReservedAugmentation();
            }

            needShowAugmentationOnNextScene = false;
        }
    }

    private IEnumerator DelayedBind(string sceneName)
    {
        yield return null;

        BindSceneUI();
        ApplySceneDefaultState(sceneName);
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
        shopUIManager = rootUI.GetComponentInChildren<ShopUIManager>(true);
        itemUIManager = rootUI.GetComponentInChildren<ItemUIManager>(true);
        inGameShopUIManager = rootUI.GetComponentInChildren<InGameShopUIManager>(true);
        inventoryManager = rootUI.GetComponentInChildren<InventoryManager>(true);

        //Debug.Log(itemUIManager == null ? "ItemUIManager 못찾음" : "ItemUIManager 찾음");

        if (playerUIManager != null && systemUI != null)
            playerUIManager.BindPlayerUI(systemUI);

        if (augUIManager != null && systemUI != null)
            augUIManager.BindAugUI(systemUI);

        if (shopUIManager != null && systemUI != null)
            shopUIManager.BindShopUI(systemUI);

        if (itemUIManager != null && systemUI != null)
            itemUIManager.BindItemUI(systemUI);

        if (inGameShopUIManager != null && systemUI != null)
            inGameShopUIManager.BindShopUI(systemUI);

        if (inventoryManager != null && systemUI != null)
            inventoryManager.BindInventoryUI(systemUI);

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
        // Main 씬
        if (sceneName == "Main")
        {
            if (mainImage != null) mainImage.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);
            if (optionPanel != null) optionPanel.SetActive(false);
            if (exitPanel != null) exitPanel.SetActive(false);

            // 메인 씬에서는 증강 UI 열리지 않게 강제 차단
            if (augUIManager != null)
            {
                augUIManager.ResetUIStateForRestart();
            }

            return;
        }

        // 게임 씬
        if (mainImage != null) mainImage.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);

        // 게임 씬 시작 시 메인용 옵션/종료 패널은 꺼둠
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
        if (playerUIManager == null) return;

        // 증강창이 열려 있는 상태에서 ESC를 누르면
        // 증강창을 잠시 숨기고 ESC 패널을 띄움
        if (augUIManager != null && augUIManager.IsAugmentationVisible())
        {
            augUIManager.HideCurrentAugmentationUI();
            playerUIManager.OpenEscPanelFromAugmentation();
            return;
        }

        if (inGameShopUIManager != null && inGameShopUIManager.IsShopOpen())
        {
            inGameShopUIManager.ToggleShop(); // 닫기
            return; //  여기서 끝
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen())
        {
            InventoryManager.Instance.ToggleInventory();
            return;
        }
        // 그 외는 PlayerUIManager에서 처리
        playerUIManager.HandleEscape();
    }

    #endregion

    #region Main Scene Button Methods

    /// <summary>
    /// 새 게임 시작
    /// - 런 초기화
    /// - 다음 씬에서 플레이어 초기화 예약
    /// - 다음 씬에서 증강창 오픈 예약
    /// - TestScene 로드
    /// </summary>
    public void StartGame()
    {
        Time.timeScale = 1f;

        // 증강/런 정보 초기화
        if (AugmentRunManager.Instance != null)
        {
            AugmentRunManager.Instance.ResetRun();
        }

        // 혹시 현재 씬에 augUI가 있다면 UI 상태 정리
        if (augUIManager != null)
        {
            augUIManager.ResetUIStateForRestart();
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ResetInventory();

        if (playerUIManager != null)
            playerUIManager.ResetEquipmentUI();

        

        // 다음 씬 로드 후 처리 예약
        needResetPlayerOnNextScene = true;
        needShowAugmentationOnNextScene = true;

        SceneManager.LoadScene("Tutorial_Stage");
        InGameShopUIManager.Instance.ResetShop();

    }

    /// <summary>
    /// 현재 게임 씬 재시작
    /// </summary>
    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;

        if (AugmentRunManager.Instance != null)
        {
            AugmentRunManager.Instance.ResetRun();
        }

        if (augUIManager != null)
        {
            augUIManager.ResetUIStateForRestart();
        }

        needResetPlayerOnNextScene = true;
        needShowAugmentationOnNextScene = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 메인 씬으로 돌아가기
    /// </summary>
    public void GoToMainScene()
    {
        Time.timeScale = 1f;

        if (AugmentRunManager.Instance != null)
        {
            AugmentRunManager.Instance.ResetRun();
        }

        if (augUIManager != null)
        {
            augUIManager.ResetUIStateForRestart();
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

        SceneManager.LoadScene("Main");
        Cursor.visible = true; //*
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

    
    public ShopUIManager GetShopUI()
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
    
    public void RequestStageEntryUI()
    {
        needShowAugmentationOnNextScene = true;
    }

}