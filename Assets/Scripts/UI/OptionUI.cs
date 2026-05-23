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
    [SerializeField] private Button escToOptionCloseBtn;

    [SerializeField] private Slider bgmSlider; 
    [SerializeField] private Slider sfxSlider; 
    [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;

    //사운드
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverSound;
    

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
    /*private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("OptionSettings ScriptableObject를 연결해주세요!");
            return;
        }

        controlViewButton.onClick.AddListener(OpenControlView);
        controlViewCloseButton.onClick.AddListener(CloseControlView);

        // 추가
        if (escToOptionCloseBtn != null)
            escToOptionCloseBtn.onClick.AddListener(OnClickEscToOptionClose);

        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 슬라이더 기본값 적용
        bgmSlider.value = settings.bgmVolume;
        sfxSlider.value = settings.sfxVolume;

        // AudioMixer에도 즉시 반영
        SetBGMVolume(settings.bgmVolume);
        SetSFXVolume(settings.sfxVolume);

        InitResolutionDropdown();
        InitScreenModeDropdown();
        InitBrightness();

        ApplySettings();
    }*/
    private void Start()
    {
        controlViewButton.onClick.AddListener(OpenControlView);
        controlViewCloseButton.onClick.AddListener(CloseControlView);

        if (escToOptionCloseBtn != null)
            escToOptionCloseBtn.onClick.AddListener(OnClickEscToOptionClose);
    }
    private void SetBGMVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        settings.bgmVolume = value;

        float db = Mathf.Log10(value) * 20;
        audioMixer.SetFloat("BGMVolume", db);
    }

    private void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        settings.sfxVolume = value;

        float db = Mathf.Log10(value) * 20;
        audioMixer.SetFloat("SFXVolume", db);
    }
    private void OnClickEscToOptionClose()
    {
        Debug.Log("[OptionUI] escToOptionCloseBtn 클릭됨");

        // Option 닫기
        gameObject.SetActive(false);

        // ESC 패널 열기
        PlayerUIManager.Instance.EnterEsc();
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

           // if (escToOptionCloseBtn != null)
          //      escToOptionCloseBtn.SetActive(false);

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

           // if (escToOptionCloseBtn != null)
            //    escToOptionCloseBtn.SetActive(false);
            
            return true;
        }

        return false;
    }

    public void InitOptionUIFromManager()
    {
        if (settings == null)
        {
            Debug.LogError("OptionSettings ScriptableObject를 연결해주세요!");
            return;
        }

        // 슬라이더 이벤트 연결
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 기본값 적용
        bgmSlider.value = settings.bgmVolume;
        sfxSlider.value = settings.sfxVolume;

        // AudioMixer 즉시 반영
        SetBGMVolume(settings.bgmVolume);
        SetSFXVolume(settings.sfxVolume);

        // 나머지 UI 초기화
        InitResolutionDropdown();
        InitScreenModeDropdown();
        InitBrightness();

        ApplySettings();
    }
}