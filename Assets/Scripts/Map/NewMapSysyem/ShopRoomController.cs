using UnityEngine;

/// <summary>
/// 상점방 전체 흐름을 관리하는 컨트롤러.
/// 
/// 흐름:
/// 1. 방 시작
/// 2. 모든 문 잠금
/// 3. 필요한 입구 위치에 플레이어 배치
/// 4. 상점 아이템 배치
/// 5. 현재 진행도에 따라 출구 설정
/// 
/// 상점방 규칙:
/// - 상점 UI를 자동으로 열지 않는다.
/// - 바닥 오브젝트를 직접 줍는 방식이다.
/// - 상점방에 들어오면 출구는 자동으로 열린다.
/// - 보스 직전이면 보스방 출구 하나만 연다.
/// - 보스 직전이 아니면 전투방 출구를 모두 연다.
/// </summary>
public class ShopRoomController : MapRoomControllerBase
{
    [Header("상점 아이템 스포너")]
    // [SerializeField] private ShopItemSpawner shopItemSpawner;

    protected override string RoomDebugName => "상점방";

    /// <summary>
    /// 상점방 전용 자동 바인딩.
    /// 
    /// 현재 ShopItemSpawner를 주석 처리한 상태라
    /// 실제로 추가 바인딩할 대상은 없다.
    /// 
    /// 나중에 ShopItemSpawner를 다시 쓸 경우
    /// 여기에서 GetComponentInChildren으로 연결하면 된다.
    /// </summary>
    protected override void AutoBindRoomReferences()
    {
        // if (shopItemSpawner == null)
        // {
        //     shopItemSpawner = GetComponentInChildren<ShopItemSpawner>(true);
        // }
    }

    private void Start()
    {
        AutoBind();

        LockAllGates();

        SpawnPlayerAtRequiredGate();

        // if (shopItemSpawner != null)
        // {
        //     shopItemSpawner.SpawnShopItems();
        // }

        SetupShopGates();
    }

    /// <summary>
    /// 현재 맵 진행도에 따라 상점방 출구를 설정한다.
    /// 
    /// 보스 직전이면:
    /// - 보스방으로 가는 문 하나만 열기
    /// 
    /// 보스 직전이 아니면:
    /// - 전투방으로 가는 문 전체 열기
    /// </summary>
    private void SetupShopGates()
    {
        if (MapFlowManager.Instance == null) return;

        if (MapFlowManager.Instance.ShouldShopConnectToBoss())
        {
            OpenOnlyOneBossGate();
        }
        else
        {
            OpenAllCombatGates();
        }
    }

    /// <summary>
    /// 상점방에서 일반 전투방으로 이어지는 모든 출구를 연다.
    /// 
    /// 상점방 이후 전투방은 보상 선택이 아니라
    /// RewardType.None으로 처리한다.
    /// </summary>
    private void OpenAllCombatGates()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[ShopRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        foreach (GateController gate in gates)
        {
            if (gate == null) continue;

            gate.SetRoute(RoomKind.Combat, RewardType.None);
            gate.SetOpen(true);
        }
    }

    /// <summary>
    /// 보스 직전 상점방일 경우,
    /// 보스방으로 가는 출구 하나만 랜덤으로 연다.
    /// </summary>
    private void OpenOnlyOneBossGate()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[ShopRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        int bossGateIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;

            if (i == bossGateIndex)
            {
                gates[i].SetRoute(RoomKind.Boss, RewardType.None);
                gates[i].SetOpen(true);
            }
            else
            {
                gates[i].SetOpen(false);
            }
        }
    }
}