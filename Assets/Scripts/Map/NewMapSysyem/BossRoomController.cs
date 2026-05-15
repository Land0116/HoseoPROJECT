using UnityEngine;

/// <summary>
/// 보스방 전체 흐름을 관리하는 컨트롤러.
/// 
/// 흐름:
/// 1. 방 시작
/// 2. 모든 문 잠금
/// 3. 필요한 입구 위치에 플레이어 배치
/// 4. 보스 생성
/// 5. 보스 사망
/// 6. 다음 액트로 가는 출구 하나만 열기
/// </summary>
public class BossRoomController : MapRoomControllerBase
{
    [Header("보스 스포너")]
    [SerializeField] private BossSpawner bossSpawner;

    protected override string RoomDebugName => "보스방";

    /// <summary>
    /// 보스방 전용 자동 바인딩.
    /// 
    /// 공통 바인딩:
    /// - gates
    /// - player
    /// 
    /// 보스방 전용 바인딩:
    /// - bossSpawner
    /// </summary>
    protected override void AutoBindRoomReferences()
    {
        if (bossSpawner == null)
        {
            bossSpawner = GetComponentInChildren<BossSpawner>(true);
        }
    }

    private void Start()
    {
        AutoBind();

        LockAllGates();

        SpawnPlayerAtRequiredGate();

        if (bossSpawner != null)
        {
            bossSpawner.SpawnBoss();
        }
        else
        {
            Debug.LogWarning("[BossRoomController] BossSpawner가 없음");
        }
    }

    /// <summary>
    /// 보스가 죽었을 때 BossSpawner 또는 Boss 쪽에서 호출해야 하는 함수.
    /// 
    /// 보스 처치 후에는 모든 문을 여는 게 아니라
    /// 다음 액트로 가는 문 하나만 랜덤으로 연다.
    /// </summary>
    public void OnBossDead()
    {
        OpenOnlyOneNextActGate();
    }

    /// <summary>
    /// 보스방 클리어 후 다음 액트 전투방으로 이동하는 문 하나만 연다.
    /// 
    /// 현재 구조에서는 BossRoom에서 문에 들어가면
    /// GateController가 MapFlowManager.CompleteBossAndGoNextAct()를 호출한다.
    /// </summary>
    private void OpenOnlyOneNextActGate()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[BossRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        int openIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;

            if (i == openIndex)
            {
                gates[i].SetRoute(RoomKind.Combat, RewardType.None);
                gates[i].SetOpen(true);
            }
            else
            {
                gates[i].SetOpen(false);
            }
        }
    }
}