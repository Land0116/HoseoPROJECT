using System.Collections;
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
    [Header("출입구")] [SerializeField] protected GateController[] gates;

    [Header("플레이어")] [SerializeField] protected Transform player;

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
        if (gates == null || gates.Length == 0) return;

        // 중요:
        // 기존 player 필드를 믿지 말고 매번 현재 PlayerController.Instance를 기준으로 잡는다.
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            player = AutoBindUtility.FindPlayerTransform();
        }

        if (player == null)
        {
           // Debug.LogWarning("[Room Spawn] Player를 찾지 못함");
            return;
        }

        GateGroup requiredGroup = MapFlowManager.Instance.RequiredEntranceGroup;

        //Debug.Log($"[Room Spawn] RequiredEntranceGroup={requiredGroup}");

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;

            //Debug.Log(
            //    $"[Room Spawn] Gate[{i}] Name={gates[i].name}, Group={gates[i].GateGroup}"
           // );
        }

        GateController[] targetGates = System.Array.FindAll(
            gates,
            gate => gate != null && gate.GateGroup == requiredGroup
        );

        if (targetGates.Length <= 0)
        {
            //Debug.LogWarning($"[Room Spawn] 현재 방에 {requiredGroup} 입구가 없음");
            return;
        }

        GateController selectedGate = targetGates[Random.Range(0, targetGates.Length)];
        Vector3 spawnPosition = selectedGate.GetPlayerSpawnPosition();

        PlayerController playerController = PlayerController.Instance;

        if (playerController != null)
        {
            playerController.TeleportToMapSpawnPosition(spawnPosition);
        }
        else
        {
            player.position = spawnPosition;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.position = spawnPosition;
            }

            Physics2D.SyncTransforms();
        }

        //Debug.Log(
    //        $"[Room Spawn] SelectedGate={selectedGate.name}, " +
        //    $"SelectedGroup={selectedGate.GateGroup}, " +
        //    $"SpawnPosition={spawnPosition}, " +
        //    $"ActualPlayerPosition={player.position}"
       // );

        StartCoroutine(VerifyPlayerSpawnPositionNextFrame(spawnPosition, selectedGate));
    }
    private IEnumerator VerifyPlayerSpawnPositionNextFrame(Vector3 expectedPosition, GateController selectedGate)
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        if (PlayerController.Instance == null) yield break;

        Vector3 currentPosition = PlayerController.Instance.transform.position;
        float distance = Vector3.Distance(currentPosition, expectedPosition);

        /*Debug.Log(
            $"[Room Spawn Verify] Expected={expectedPosition}, " +
            $"Actual={currentPosition}, Distance={distance}, " +
            $"SelectedGate={selectedGate.name}"
        );*/

        if (distance > 0.1f)
        {
            //Debug.Log(
               // "[Room Spawn Verify] 플레이어 위치가 스폰 직후 다른 곳으로 덮어써짐. 다시 보정함."
           // );

            PlayerController.Instance.TeleportToMapSpawnPosition(expectedPosition);
        }
    }
}