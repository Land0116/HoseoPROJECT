using System.Collections;
using UnityEngine;

public class BossRoomController : MapRoomControllerBase
{
    [Header("보스 스포너")]
    [SerializeField] private BossSpawner bossSpawner;

    [Header("컷씬 설정")]
    [SerializeField] private int stageNumberForCutscene = 1;
    [SerializeField] private int finalStageNumber = 3;

    protected override string RoomDebugName => "보스방";

    private bool isBossCleared = false;
    private bool isFinalClearGateUsed = false;

    protected override void AutoBindRoomReferences()
    {
        if (bossSpawner == null)
        {
            bossSpawner = GetComponentInChildren<BossSpawner>(true);
        }
    }

    private IEnumerator Start()
    {
        AutoBind();

        LockAllGates();
        SpawnPlayerAtRequiredGate();

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.ShowCutscenePanel();
            VideoCutsceneManager.Instance.BringCutsceneToFront();
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            yield return VideoCutsceneManager.Instance.PlayCutsceneForExternalFade(
                VideoCutsceneType.StageBossIntro,
                stageNumberForCutscene,
                false
            );
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.EndExternalFadeCutscene();
        }

        SpawnBoss();

        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlackAndUnlockPlayer();
        }
    }

    private void SpawnBoss()
    {
        if (bossSpawner != null)
        {
            bossSpawner.SpawnBoss();
        }
        else
        {
            Debug.LogWarning("[BossRoomController] BossSpawner가 없음");
        }
    }

    public void OnBossDead()
    {
        if (isBossCleared) return;

        isBossCleared = true;

        StartCoroutine(BossClearRoutine());
    }

    private IEnumerator BossClearRoutine()
    {
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
        }

        bool isFinalStage = stageNumberForCutscene >= finalStageNumber;

        /*
         * 핵심 변경:
         * 마지막 스테이지 보스 처치 시 Ending 컷씬을 바로 재생하지 않는다.
         * 대신 클리어 게이트를 열고, 플레이어가 게이트를 밟았을 때 엔딩 컷씬을 재생한다.
         */
        if (isFinalStage)
        {
            OpenOnlyOneFinalClearGate();

            if (MapTransitionManager.Instance != null)
            {
                yield return MapTransitionManager.Instance.FadeFromBlackAndUnlockPlayer();
            }

            yield break;
        }

        /*
         * 일반 보스 클리어는 기존처럼 클리어 컷씬 후 다음 액트 게이트 개방.
         */
        yield return PlayBossClearCutsceneWithFade(VideoCutsceneType.StageBossClear);

        OpenOnlyOneNextActGate();

        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlackAndUnlockPlayer();
        }
    }

    private IEnumerator PlayBossClearCutsceneWithFade(VideoCutsceneType cutsceneType)
    {
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.ShowCutscenePanel();
            VideoCutsceneManager.Instance.BringCutsceneToFront();
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            yield return VideoCutsceneManager.Instance.PlayCutsceneForExternalFade(
                cutsceneType,
                stageNumberForCutscene,
                false
            );
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.EndExternalFadeCutscene();
        }
    }

    private void OpenOnlyOneNextActGate()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[BossRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        int nextActGateIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            GateController gate = gates[i];

            if (gate == null) continue;

            if (i == nextActGateIndex)
            {
                gate.SetRoute(RoomKind.Combat, RewardType.None);
                gate.SetOpen(true);
            }
            else
            {
                gate.SetRoute(RoomKind.Combat, RewardType.None);
                gate.SetOpen(false);
            }
        }

        Debug.Log("[BossRoomController] 보스 클리어. 다음 액트 출구 1개 개방.");
    }

    private void OpenOnlyOneFinalClearGate()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[BossRoomController] 열 수 있는 최종 클리어 Gate가 없음");
            return;
        }

        int clearGateIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            GateController gate = gates[i];

            if (gate == null) continue;

            if (i == clearGateIndex)
            {
                gate.SetRoute(RoomKind.Clear, RewardType.None);
                gate.SetOpen(true);
            }
            else
            {
                gate.SetRoute(RoomKind.Combat, RewardType.None);
                gate.SetOpen(false);
            }
        }

        Debug.Log("[BossRoomController] 최종 보스 클리어. 엔딩 컷씬 게이트 1개 개방.");
    }

    public void TryEnterFinalClearGate()
    {
        if (!isBossCleared)
            return;

        if (stageNumberForCutscene < finalStageNumber)
            return;

        if (isFinalClearGateUsed)
            return;

        isFinalClearGateUsed = true;

        StartCoroutine(FinalClearGateRoutine());
    }

    private IEnumerator FinalClearGateRoutine()
    {
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.ShowCutscenePanel();
            VideoCutsceneManager.Instance.BringCutsceneToFront();
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            yield return VideoCutsceneManager.Instance.PlayCutsceneForExternalFade(
                VideoCutsceneType.Ending,
                stageNumberForCutscene,
                false
            );
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.EndExternalFadeCutscene();
        }

        ShowGameClearUI();

        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlack();
            MapTransitionManager.Instance.SetPlayerInputLocked(false);
        }
    }

    private void ShowGameClearUI()
    {
        Debug.Log("[BossRoomController] 게임 클리어 UI 표시");

        /*
         * 여기에는 네 클리어 UI를 연결하면 됨.
         * 예:
         * PlayerUIManager.Instance.ShowGameClearUI();
         *
         * 아직 함수가 없다면 PlayerUIManager에 ShowGameClearUI()를 만들어서
         * ClearPanel을 켜면 됨.
         */

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowGameClearUI();
        }
    }
}