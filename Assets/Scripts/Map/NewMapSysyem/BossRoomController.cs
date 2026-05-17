using System.Collections;
using UnityEngine;

public class BossRoomController : MapRoomControllerBase
{
    [Header("보스 스포너")]
    [SerializeField] private BossSpawner bossSpawner;

    [Header("보스 입장 컷씬")]
    [SerializeField] private BossIntroCutscenePlayer cutscenePlayer;

    protected override string RoomDebugName => "보스방";

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

        if (cutscenePlayer != null)
        {
            yield return cutscenePlayer.PlayCutsceneSequence();
        }

        if (MapTransitionManager.Instance != null)
        {
            yield return MapTransitionManager.Instance.FadeFromBlackAndUnlockPlayer();
        }

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
        StartCoroutine(BossClearRoutine());
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