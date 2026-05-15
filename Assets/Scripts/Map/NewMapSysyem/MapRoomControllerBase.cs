using UnityEngine;

/// <summary>
/// 전투방, 상점방, 보스방 컨트롤러가 공통으로 사용하는 부모 클래스.
/// 
/// 공통 처리:
/// - GateController 배열 자동 바인딩
/// - 플레이어 Transform 자동 바인딩
/// - 모든 출입구 잠금
/// - 모든 출입구 열기
/// - MapFlowManager.RequiredEntranceGroup 기준으로 플레이어 위치 배치
/// 
/// 자식 클래스는 AutoBindRoomReferences()만 override해서
/// 몬스터 스포너, 보스 스포너, 보상 스포너 같은 자기 전용 참조만 연결하면 된다.
/// </summary>
public abstract class MapRoomControllerBase : AutoBindableBehaviour
{
    [Header("출입구")]
    [SerializeField] protected GateController[] gates;

    [Header("플레이어")]
    [SerializeField] protected Transform player;

    /// <summary>
    /// AutoBindableBehaviour의 실제 바인딩 함수.
    /// 
    /// 이 함수는 sealed로 막아둔다.
    /// 이유:
    /// 방 컨트롤러들은 무조건 공통 바인딩을 먼저 해야 하기 때문.
    /// 
    /// 자식 클래스는 이 함수를 직접 override하지 말고
    /// AutoBindRoomReferences()만 override하면 된다.
    /// </summary>
    protected sealed override void AutoBindCore()
    {
        AutoBindCommonRoomReferences();

        AutoBindRoomReferences();
    }

    /// <summary>
    /// 방 컨트롤러 공통 참조 자동 연결.
    /// </summary>
    private void AutoBindCommonRoomReferences()
    {
        if (AutoBindUtility.IsNullOrEmptyOrContainsNull(gates))
        {
            gates = GetComponentsInChildren<GateController>(true);
        }

        if (Application.isPlaying && player == null)
        {
            player = AutoBindUtility.FindPlayerTransform();
        }
    }

    /// <summary>
    /// 자식 방 컨트롤러에서 추가 바인딩할 때 override한다.
    /// 
    /// 예:
    /// - CombatRoomController: MonsterSpawner, RewardObjectSpawner
    /// - BossRoomController: BossSpawner
    /// - ShopRoomController: ShopItemSpawner
    /// </summary>
    protected virtual void AutoBindRoomReferences()
    {
    }

    /// <summary>
    /// 디버그 메시지에 사용할 방 이름.
    /// 자식 클래스에서 override해서 "전투방", "상점방", "보스방"처럼 표시한다.
    /// </summary>
    protected virtual string RoomDebugName => "방";

    /// <summary>
    /// 현재 방의 모든 출입구를 닫는다.
    /// 
    /// 문을 닫으면:
    /// - GateCollider 비활성화
    /// - BlockObject 활성화
    /// - 마커 제거
    /// </summary>
    protected void LockAllGates()
    {
        if (gates == null) return;

        foreach (GateController gate in gates)
        {
            if (gate == null) continue;

            gate.SetOpen(false);
        }
    }

    /// <summary>
    /// 현재 방의 모든 출입구를 연다.
    /// 
    /// 문을 열면:
    /// - GateCollider 활성화
    /// - BlockObject 비활성화
    /// - 현재 route에 맞는 마커 생성
    /// </summary>
    protected void OpenAllGates()
    {
        if (gates == null) return;

        foreach (GateController gate in gates)
        {
            if (gate == null) continue;

            gate.SetOpen(true);
        }
    }

    /// <summary>
    /// MapFlowManager가 요구하는 입구 그룹 기준으로
    /// 플레이어를 해당 출입구의 playerSpawnPoint 위치로 이동시킨다.
    /// 
    /// 예:
    /// 이전 방에서 A문으로 나갔다면
    /// 다음 방에서는 반대쪽 B 입구로 들어오는 식이다.
    /// </summary>
    protected void SpawnPlayerAtRequiredGate()
    {
        if (MapFlowManager.Instance == null) return;
        if (player == null) return;
        if (gates == null || gates.Length == 0) return;

        GateGroup requiredGroup = MapFlowManager.Instance.RequiredEntranceGroup;

        GateController[] targetGates = System.Array.FindAll(
            gates,
            gate => gate != null && gate.GateGroup == requiredGroup
        );

        if (targetGates.Length <= 0)
        {
            Debug.LogWarning($"현재 {RoomDebugName}에 {requiredGroup} 입구가 없음");
            return;
        }

        GateController selectedGate = targetGates[Random.Range(0, targetGates.Length)];

        player.position = selectedGate.GetPlayerSpawnPosition();
    }
}