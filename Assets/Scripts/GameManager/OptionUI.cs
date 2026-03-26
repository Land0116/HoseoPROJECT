using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    private Resolution[] resolutions =
    {
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2560, height = 1440 }
    };

    private void Awake()
    {
        
        SceneManager.sceneLoaded += OnSceneLoaded;
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
            case 2: mode = FullScreenMode.FullScreenWindow; break;
        }
        Screen.SetResolution(res.width, res.height, mode);

        UpdateBrightnessOverlay();
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
}