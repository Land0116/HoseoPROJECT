using System.Collections;
using UnityEngine;

public class BossRoomController : MapRoomControllerBase
{
    [Header("보스 스포너")]
    [SerializeField] private BossSpawner bossSpawner;

    [Header("보스 입장 컷씬")]
    [SerializeField] private BossIntroCutscenePlayer cutscenePlayer;

    protected override string RoomDebugName => "보스방";
    private bool isBossCleared = false;

    protected override void AutoBindRoomReferences()
    {
        if (bossSpawner == null)
        {
            bossSpawner = GetComponentInChildren<BossSpawner>(true);
        }

        if (cutscenePlayer == null)
        {
            cutscenePlayer = FindAnyObjectByType<BossIntroCutscenePlayer>();
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

        // 1. 컷씬 패널을 먼저 켜둔다.
        // 단, 아직 TransitionPanel이 검은 화면으로 덮고 있으므로 플레이어에게는 안 보인다.
        if (cutscenePlayer != null)
        {
            cutscenePlayer.ShowCutscenePanel();
        }

        // 2. 검은 화면을 걷으면서 컷씬 패널이 보이게 만든다.
        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlack();
        }

        // 3. 컷씬 패널을 최상단으로 올리고 영상 재생
        if (cutscenePlayer != null)
        {
            cutscenePlayer.BringCutsceneToFront();
            yield return cutscenePlayer.PlayCutsceneSequence();
        }

        // 4. 컷씬이 끝나면 다시 검은 화면으로 덮는다.
        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        // 5. 컷씬 패널 끄기
        if (cutscenePlayer != null)
        {
            cutscenePlayer.HideCutscenePanel();
        }

        // 6. 검은 화면을 걷고 조작 가능 상태로 복귀
        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlackAndUnlockPlayer();
        }

        // 7. 보스 생성
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

        OpenOnlyOneNextActGate();
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
                // 보스방에서 열린 문은 다음 액트 전투방으로 가는 문이다.
                // 실제 다음 액트 이동은 GateController가 CurrentRoomKind == Boss일 때 처리한다.
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
    
    private IEnumerator BossClearRoutine()
    {
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.SetPlayerInputLocked(true);
            yield return MapTransitionManager.Instance.FadeToBlack();
        }

        if (MapFlowManager.Instance != null)
        {
            MapFlowManager.Instance.CompleteBossAndGoNextAct();
        }
    }
}