using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점방 전체 흐름을 관리하는 컨트롤러.
/// 
/// 상점방 규칙:
/// - 상점 UI를 자동으로 열지 않는다.
/// - 바닥 오브젝트를 직접 줍는 방식이다.
/// - 상점방에 들어오면 출구는 자동으로 열린다.
/// - 보스 직전이면 보스방 출구 하나만 연다.
/// - 보스 직전이 아니면 출구 4개 중 1개는 닫고,
///   나머지 3개를 증강 / 아이템 / 스킬 전투방 출구로 연다.
/// </summary>
public class ShopRoomController : MapRoomControllerBase
{
    [Header("상점 아이템 스포너")]
    // [SerializeField] private ShopItemSpawner shopItemSpawner;

    protected override string RoomDebugName => "상점방";

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

        // 상점 아이템을 필드에 배치하는 구조를 쓸 경우 여기서 호출
        // if (shopItemSpawner != null)
        // {
        //     shopItemSpawner.SpawnShopItems();
        // }

        SetupShopGates();
    }

    private void SetupShopGates()
    {
        if (MapFlowManager.Instance == null) return;

        if (MapFlowManager.Instance.ShouldShopConnectToBoss())
        {
            // currentCombatRoomNumber가 6 이상이면
            // 출구 하나만 보스룸으로 연다.
            OpenOnlyOneBossGate();
        }
        else
        {
            // 일반 상점방이면
            // 출구 4개 중 1개 닫고,
            // 나머지 3개에 Augment / Item / Skill 배정
            OpenRewardCombatGates();
        }
    }

    /// <summary>
    /// 일반 상점방 출구 설정.
    /// 
    /// 조건:
    /// - Gate 4개 중 1개는 닫는다.
    /// - 나머지 3개는 Combat으로 연결한다.
    /// - 보상은 Augment / Item / Skill을 하나씩 배정한다.
    /// - 상점방에서 상점 출구는 절대 배정하지 않는다.
    /// </summary>
    private void OpenRewardCombatGates()
    {
        if (gates == null || gates.Length <= 0)
        {
            Debug.LogWarning("[ShopRoomController] 열 수 있는 Gate가 없음");
            return;
        }

        List<GateController> validGates = new List<GateController>();

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] != null)
            {
                validGates.Add(gates[i]);
            }
        }

        if (validGates.Count <= 0)
        {
            Debug.LogWarning("[ShopRoomController] 유효한 Gate가 없음");
            return;
        }

        if (validGates.Count < 4)
        {
            Debug.LogWarning("[ShopRoomController] 상점방은 Gate 4개 기준임. 현재 Gate 수: " + validGates.Count);
        }

        int closedGateIndex = Random.Range(0, validGates.Count);

        List<RewardType> rewardTypes = new List<RewardType>
        {
            RewardType.Augment,
            RewardType.Item,
            RewardType.Skill
        };

        ShuffleRewards(rewardTypes);

        int rewardIndex = 0;

        for (int i = 0; i < validGates.Count; i++)
        {
            GateController gate = validGates[i];

            if (i == closedGateIndex)
            {
                gate.SetOpen(false);
                continue;
            }

            // 상점방에서 열리는 출구는 전부 다음 전투방으로 연결된다.
            // 단, 각 출구의 RewardType은 다음 전투방 클리어 후 받을 보상이다.
            RewardType rewardType = rewardTypes[rewardIndex % rewardTypes.Count];

            gate.SetRoute(RoomKind.Combat, rewardType);
            gate.SetOpen(true);

            rewardIndex++;
        }
    }

    private void ShuffleRewards(List<RewardType> rewardTypes)
    {
        for (int i = 0; i < rewardTypes.Count; i++)
        {
            int randomIndex = Random.Range(i, rewardTypes.Count);

            RewardType temp = rewardTypes[i];
            rewardTypes[i] = rewardTypes[randomIndex];
            rewardTypes[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 보스 직전 상점방일 경우,
    /// 보스방으로 가는 출구 하나만 랜덤으로 연다.
    /// 나머지는 닫는다.
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
            GateController gate = gates[i];

            if (gate == null) continue;

            if (i == bossGateIndex)
            {
                // 6번 상점방에서는 이 출구 하나만 보스룸으로 연결
                gate.SetRoute(RoomKind.Boss, RewardType.None);
                gate.SetOpen(true);
            }
            else
            {
                // 나머지 3개는 완전히 닫힘
                gate.SetRoute(RoomKind.Combat, RewardType.None);
                gate.SetOpen(false);
            }
        }
    }
}