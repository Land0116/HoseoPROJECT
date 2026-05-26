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
    private bool isBossClearGateUsed = false;

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

        // 보스룸 입장 컷씬
        yield return PlayCutsceneWithFade(VideoCutsceneType.StageBossIntro);

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
        if (isBossCleared)
            return;

        isBossCleared = true;

        // 중요:
        // 보스가 죽었다고 컷씬을 바로 재생하지 않는다.
        // 게이트만 열고, 컷씬은 게이트를 밟았을 때 재생한다.
        OpenOnlyOneBossClearGate();
    }

    private void OpenOnlyOneBossClearGate()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[BossRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        bool isFinalStage = stageNumberForCutscene >= finalStageNumber;
        int openGateIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            GateController gate = gates[i];

            if (gate == null)
                continue;

            if (i == openGateIndex)
            {
                if (isFinalStage)
                {
                    // 마지막 스테이지 보스 클리어 게이트
                    gate.SetRoute(RoomKind.Clear, RewardType.None);
                }
                else
                {
                    // 일반 보스 클리어 후 다음 액트 이동 게이트
                    gate.SetRoute(RoomKind.Combat, RewardType.None);
                }

                gate.SetOpen(true);
            }
            else
            {
                gate.SetRoute(RoomKind.Combat, RewardType.None);
                gate.SetOpen(false);
            }
        }

        if (isFinalStage)
        {
            Debug.Log("[BossRoomController] 최종 보스 처치. 엔딩 게이트 1개 개방.");
        }
        else
        {
            Debug.Log("[BossRoomController] 보스 처치. 다음 액트 게이트 1개 개방.");
        }
    }

    public void TryEnterBossClearGate(GateGroup usedGateGroup)
    {
        if (!isBossCleared)
            return;

        if (isBossClearGateUsed)
            return;

        isBossClearGateUsed = true;

        StartCoroutine(BossClearGateRoutine(usedGateGroup));
    }

    private IEnumerator BossClearGateRoutine(GateGroup usedGateGroup)
    {
        bool isFinalStage = stageNumberForCutscene >= finalStageNumber;

        VideoCutsceneType cutsceneType = isFinalStage
            ? VideoCutsceneType.Ending
            : VideoCutsceneType.StageBossClear;

        // 게이트를 밟았을 때 컷씬 재생
        yield return PlayCutsceneWithFade(cutsceneType);

        if (isFinalStage)
        {
            ShowGameClearUI();

            if (MapTransitionManager.Instance != null)
            {
                yield return MapTransitionManager.Instance.FadeFromBlack();

                // MapTransition 잠금만 해제.
                // GameClearPanel 잠금은 PlayerUIManager.ShowGameClearUI()에서 유지됨.
                MapTransitionManager.Instance.SetPlayerInputLocked(false);
            }

            yield break;
        }

        // 일반 보스 클리어 컷씬이 끝난 뒤 다음 액트 이동
        if (MapFlowManager.Instance != null)
        {
            MapFlowManager.Instance.CompleteBossAndGoNextAct(usedGateGroup);
        }
        else
        {
            Debug.LogWarning("[BossRoomController] MapFlowManager가 없음");
        }
    }

    private IEnumerator PlayCutsceneWithFade(VideoCutsceneType cutsceneType)
    {
        // 1. 검은 화면으로 덮기
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        // 2. 컷씬 패널 켜기
        if (VideoCutsceneManager.Instance != null)
        {
            VideoCutsceneManager.Instance.ShowCutscenePanel();
            VideoCutsceneManager.Instance.BringCutsceneToFront();
        }
        else
        {
            Debug.LogWarning("[BossRoomController] VideoCutsceneManager가 없음");
            yield break;
        }

        // 3. 검은 화면 걷어서 컷씬 보이게 하기
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        // 4. 컷씬 재생
        yield return VideoCutsceneManager.Instance.PlayCutsceneForExternalFade(
            cutsceneType,
            stageNumberForCutscene,
            true
        );

        // 5. 컷씬 끝나면 다시 검은 화면으로 덮기
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.BringTransitionToFront();
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        // 6. 검은 화면 뒤에서 컷씬 패널 끄기
        VideoCutsceneManager.Instance.EndExternalFadeCutscene();
    }

    private void ShowGameClearUI()
    {
        Debug.Log("[BossRoomController] 게임 클리어 UI 표시");

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowGameClearUI();
        }
    }
}