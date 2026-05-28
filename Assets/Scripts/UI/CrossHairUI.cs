using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance { get; private set; }

    [Header("크로스헤어 기준 Rect")]
    [SerializeField] private RectTransform crosshairRect;

    [Header("표시/숨김 제어")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("크로스헤어 전용 Canvas")]
    [SerializeField] private Canvas crosshairCanvas;

    [SerializeField] private int sortingOrder = 9999;

    [Header("게임 중 시스템 커서 숨김")]
    [SerializeField] private bool hideSystemCursorInGameplay = true;

    [Header("타겟 감지")]
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField] private float detectRadius = 0.08f;

    [Header("크로스헤어 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color targetColor = Color.red;

    private Graphic[] crosshairGraphics;
    private Camera mainCamera;
    private bool isVisible;
    private bool isTargetingMonster;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoBind();
        SetupCanvas();
        CacheGraphics();
        DisableRaycastTargets();

        RefreshByScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        if (!isVisible) return;
        if (crosshairRect == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        crosshairRect.position = mouseScreenPos;

        UpdateTargetDetection(mouseScreenPos);
    }

    private void AutoBind()
    {
        if (crosshairRect == null)
            crosshairRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (crosshairCanvas == null)
            crosshairCanvas = GetComponent<Canvas>();

        mainCamera = Camera.main;
    }

    private void SetupCanvas()
    {
        if (crosshairCanvas == null)
            return;

        crosshairCanvas.overrideSorting = true;
        crosshairCanvas.sortingOrder = sortingOrder;
    }

    private void CacheGraphics()
    {
        crosshairGraphics = GetComponentsInChildren<Graphic>(true);
    }

    private void DisableRaycastTargets()
    {
        if (crosshairGraphics == null)
            return;

        for (int i = 0; i < crosshairGraphics.Length; i++)
        {
            if (crosshairGraphics[i] == null) continue;
            crosshairGraphics[i].raycastTarget = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCamera = Camera.main;
        RefreshByScene(scene.name);
    }

    private void RefreshByScene(string sceneName)
    {
        if (sceneName == "Main")
        {
            SetVisible(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        SetVisible(true);
    }

    private void UpdateTargetDetection(Vector2 mouseScreenPos)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            SetTargetingMonster(false);
            return;
        }

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(
                mouseScreenPos.x,
                mouseScreenPos.y,
                -mainCamera.transform.position.z
            )
        );

        mouseWorldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapCircle(
            mouseWorldPos,
            detectRadius,
            monsterLayerMask
        );

        SetTargetingMonster(hit != null);
    }

    private void SetTargetingMonster(bool value)
    {
        if (isTargetingMonster == value)
            return;

        isTargetingMonster = value;

        Color nextColor = isTargetingMonster ? targetColor : normalColor;
        ApplyCrosshairColor(nextColor);
    }

    private void ApplyCrosshairColor(Color color)
    {
        if (crosshairGraphics == null)
            return;

        for (int i = 0; i < crosshairGraphics.Length; i++)
        {
            if (crosshairGraphics[i] == null) continue;
            crosshairGraphics[i].color = color;
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (!visible)
        {
            SetTargetingMonster(false);
            ApplyCrosshairColor(normalColor);
        }

        Cursor.visible = visible ? !hideSystemCursorInGameplay : true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowCrosshair()
    {
        SetVisible(true);
    }

    public void HideCrosshair()
    {
        SetVisible(false);
    }
}