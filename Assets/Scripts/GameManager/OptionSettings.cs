using UnityEngine;

[CreateAssetMenu(fileName = "OptionSettings", menuName = "Settings/OptionSettings")]
public class OptionSettings : ScriptableObject
{
    public int resolutionIndex = 1; // 기본 1920x1080
    public int screenModeIndex = 2; // 기본 전체창모드
    [Range(0.1f, 1f)] public float brightness = 1f;
}