using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class OptionUI : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Screen Mode")]
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    [Header("Brightness")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Image brightnessOverlay;

    [Header("Settings Data")]
    [SerializeField] private OptionSettings settings;

    [Header("Control View")]
    [SerializeField] private GameObject controlViewPanel;
    [SerializeField] private Button controlViewButton;
    [SerializeField] private Button controlViewCloseButton;

    [Header("ESC Close")]
    [SerializeField] private GameObject escToOptionCloseBtn;

    private Resolution[] resolutions =
    {
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2560, height = 1440 }
    };
    public bool IsControlViewOpen => controlViewPanel != null && controlViewPanel.activeSelf;
    private void Awake()
    {
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void Update()
    {
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySettings();
        UpdateUIElements();
    }
    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("OptionSettings ScriptableObject를 연결해주세요!");
            return;
        }


        controlViewButton.onClick.AddListener(OpenControlView);
        controlViewCloseButton.onClick.AddListener(CloseControlView);
        InitResolutionDropdown();
        InitScreenModeDropdown();
        InitBrightness();

        ApplySettings();
    }

    private void InitResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in resolutions)
        {
            options.Add(res.width + " x " + res.height);
        }
        resolutionDropdown.AddOptions(options);

        resolutionDropdown.value = settings.resolutionIndex;
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnResolutionChanged(int index)
    {
        settings.resolutionIndex = index; 
        ApplySettings();
    }

    private void InitScreenModeDropdown()
    {
        screenModeDropdown.ClearOptions();
        List<string> options = new List<string>() { "전체화면", "창모드", "전체창모드" };
        screenModeDropdown.AddOptions(options);

        screenModeDropdown.value = settings.screenModeIndex;
        screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
    }

    private void OnScreenModeChanged(int index)
    {
        settings.screenModeIndex = index; 
        ApplySettings();
    }

    private void InitBrightness()
    {
        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.value = settings.brightness;
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
    }

    private void OnBrightnessChanged(float value)
    {
        value = Mathf.Clamp(value, 0.1f, 1f);
        settings.brightness = value; 
        UpdateBrightnessOverlay();
    }

    private void ApplySettings()
    {
        
        Resolution res = resolutions[settings.resolutionIndex];
        FullScreenMode mode = FullScreenMode.FullScreenWindow;

        switch (settings.screenModeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.Windowed; break;
            case 2: mode = FullScreenMode.ExclusiveFullScreen; break;
        }
        Screen.SetResolution(res.width, res.height, mode);
        Cursor.lockState = CursorLockMode.Confined;

        UpdateBrightnessOverlay();
    }
    public static void ApplySettingsStatic(OptionSettings settings)
    {
        if (settings == null) return;

        Resolution[] resolutions =
        {
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2560, height = 1440 }
    };

        Resolution res = resolutions[settings.resolutionIndex];

        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;

        switch (settings.screenModeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.Windowed; break;
            case 2: mode = FullScreenMode.ExclusiveFullScreen; break;
        }

        Screen.SetResolution(res.width, res.height, mode);
    }
    private void UpdateBrightnessOverlay()
    {
        if (brightnessOverlay != null)
        {
            Color color = brightnessOverlay.color;
            color.a = 1f - settings.brightness;
            brightnessOverlay.color = color;
        }
    }

    private void UpdateUIElements()
    {
        
        if (resolutionDropdown != null) resolutionDropdown.value = settings.resolutionIndex;
        if (screenModeDropdown != null) screenModeDropdown.value = settings.screenModeIndex;
        if (brightnessSlider != null) brightnessSlider.value = settings.brightness;
    }
    private void OpenControlView()
    {
        if (controlViewPanel != null)
            controlViewPanel.SetActive(true);
    }

    public void CloseControlView()
    {
        if (controlViewPanel != null)
            controlViewPanel.SetActive(false);
    }
    private bool HandleEscape()
    {
        Debug.Log($"[OptionUI] HandleEscape 호출됨 | controlViewPanel active = {controlViewPanel?.activeSelf}");

        if (controlViewPanel != null && controlViewPanel.activeSelf)
        {
            Debug.Log("[OptionUI] controlViewPanel 닫음 (ESC 소비)");
            CloseControlView();

            if (escToOptionCloseBtn != null)
                escToOptionCloseBtn.SetActive(false);

            return true;
        }

        Debug.Log("[OptionUI] ESC 소비 안함 (false 반환)");
        return false;
    }

    public bool HandleEscapeConsumed()
    {
        Debug.Log($"[OptionUI] HandleEscapeConsumed 호출됨 | controlViewPanel = {controlViewPanel?.activeSelf}");
        if (controlViewPanel != null && controlViewPanel.activeSelf)
        {
            CloseControlView();

            if (escToOptionCloseBtn != null)
                escToOptionCloseBtn.SetActive(false);

            return true;
        }

        return false;
    }
}