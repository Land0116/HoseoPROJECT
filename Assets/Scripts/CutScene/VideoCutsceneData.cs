using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public enum VideoCutsceneType
{
    None,

    GameStart,
    StageBossIntro,
    StageBossClear,
    Ending,

    Custom
}

[CreateAssetMenu(
    fileName = "VideoCutsceneData",
    menuName = "Cutscene/Video Cutscene Data"
)]
public class VideoCutsceneData : ScriptableObject
{
    [Header("컷씬 타입")]
    public VideoCutsceneType cutsceneType = VideoCutsceneType.None;

    [Header("스테이지 번호")]
    [Tooltip("0이면 공통 컷씬. 1,2,3이면 해당 스테이지 전용 컷씬.")]
    public int stageNumber = 0;

    [Header("재생할 영상")]
    public VideoClip[] videoClips;

    [Header("입력 제어")]
    public bool lockPlayerInput = true;

    [Header("재생 옵션")]
    public bool playOnce = true;
    public bool allowSkip = true;
    public Key skipKey = Key.Space;

    [Header("컷씬 종료 후 딜레이")]
    public float finishDelay = 0f;
}