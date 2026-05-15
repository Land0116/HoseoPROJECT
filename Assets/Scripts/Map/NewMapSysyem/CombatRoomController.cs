using UnityEngine;

/// <summary>
/// 전투방 전체 흐름을 관리하는 컨트롤러.
/// 
/// 흐름:
/// 1. 방 시작
/// 2. 모든 문 잠금
/// 3. 필요한 입구 위치에 플레이어 배치
/// 4. 웨이브 시작
/// 5. 모든 몬스터 처치
/// 6. 다음 웨이브 또는 보상 오브젝트 생성
/// 7. 보상 획득 완료
/// 8. 다음 경로 배정
/// 9. 모든 문 열기
/// </summary>
public class CombatRoomController : MapRoomControllerBase
{
    [Header("몬스터 스포너")]
    [SerializeField] private MonsterSpawner monsterSpawner;

    [Header("보상 오브젝트 스포너")]
    [SerializeField] private RewardObjectSpawner rewardObjectSpawner;

    [Header("웨이브 수")]
    [SerializeField] private int maxWave = 2;

    private int currentWave = 0;
    private bool isRewardFinished = false;
    private bool isRewardObjectSpawned = false;

    protected override string RoomDebugName => "전투방";

    /// <summary>
    /// 전투방 전용 자동 바인딩.
    /// 
    /// 공통 바인딩:
    /// - gates
    /// - player
    /// 
    /// 전투방 전용 바인딩:
    /// - monsterSpawner
    /// - rewardObjectSpawner
    /// </summary>
    protected override void AutoBindRoomReferences()
    {
        if (monsterSpawner == null)
        {
            monsterSpawner = GetComponentInChildren<MonsterSpawner>(true);
        }

        if (rewardObjectSpawner == null)
        {
            rewardObjectSpawner = GetComponentInChildren<RewardObjectSpawner>(true);
        }
    }

    private void Start()
    {
        // Awake에서도 AutoBind가 실행되지만,
        // Start에서 한 번 더 호출한다.
        //
        // 이유:
        // 플레이어는 씬 로드 타이밍에 따라 Awake 시점에 아직 못 찾을 수 있다.
        AutoBind();

        LockAllGates();

        SpawnPlayerAtRequiredGate();

        StartNextWave();
    }

    /// <summary>
    /// 다음 웨이브를 시작한다.
    /// 
    /// currentWave가 maxWave를 넘으면
    /// 더 이상 몬스터를 생성하지 않고 보상 오브젝트를 생성한다.
    /// </summary>
    private void StartNextWave()
    {
        currentWave++;

        if (currentWave > maxWave)
        {
            SpawnRewardObject();
            return;
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.SpawnWave(currentWave);
        }
        else
        {
            Debug.LogWarning("[CombatRoomController] MonsterSpawner가 없음");
        }
    }

    /// <summary>
    /// 몬스터 스포너 또는 몬스터 관리 쪽에서
    /// 현재 방의 모든 몬스터가 죽었을 때 호출해야 하는 함수.
    /// </summary>
    public void OnAllMonstersDead()
    {
        if (currentWave < maxWave)
        {
            StartNextWave();
            return;
        }

        SpawnRewardObject();
    }

    /// <summary>
    /// 전투 종료 후 보상 오브젝트를 생성한다.
    /// 
    /// 보상 타입은 MapFlowManager에서 가져온다.
    /// 1-1, 2-1, 3-1이면 증강 보상.
    /// 그 외에는 이전 출구 선택에서 정한 보상 타입.
    /// </summary>
    private void SpawnRewardObject()
    {
        if (isRewardObjectSpawned) return;
        if (isRewardFinished) return;
        if (MapFlowManager.Instance == null) return;

        isRewardObjectSpawned = true;

        RewardType rewardType = MapFlowManager.Instance.GetCurrentCombatRewardType();

        if (rewardType == RewardType.None)
        {
            OnRewardFinished();
            return;
        }

        if (rewardObjectSpawner != null)
        {
            rewardObjectSpawner.SpawnRewardObject(rewardType, OnRewardFinished);
        }
        else
        {
            Debug.LogWarning("[CombatRoomController] RewardObjectSpawner가 없음. 보상 없이 출구를 엶.");
            OnRewardFinished();
        }
    }

    /// <summary>
    /// 보상 획득이 완료되었을 때 호출된다.
    /// 
    /// 여기서 다음 출구들의 목적지를 배정하고
    /// 모든 문을 연다.
    /// </summary>
    private void OnRewardFinished()
    {
        if (isRewardFinished) return;

        isRewardFinished = true;

        AssignNextRoutes();

        OpenAllGates();
    }

    /// <summary>
    /// 전투방 클리어 후 다음 출구들의 경로를 배정한다.
    /// 
    /// 기본 규칙:
    /// - 출구 중 하나는 상점
    /// - 나머지 출구는 전투방
    /// - 전투방 출구에는 보상 타입을 붙인다
    /// </summary>
    private void AssignNextRoutes()
    {
        if (gates == null || gates.Length <= 0) return;

        RewardType firstRandomReward = GetRandomRewardType();
        RewardType secondRandomReward = GetRandomRewardTypeExcept(firstRandomReward);

        int shopIndex = Random.Range(0, gates.Length);

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;

            if (i == shopIndex)
            {
                gates[i].SetRoute(RoomKind.Shop, RewardType.None);
            }
        }

        int rewardIndex = 0;

        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] == null) continue;
            if (i == shopIndex) continue;
            
            if (rewardIndex == 0)
            {
                gates[i].SetRoute(RoomKind.Combat, firstRandomReward);
            }
            else
            {
                gates[i].SetRoute(RoomKind.Combat, secondRandomReward);
            }

            rewardIndex++;
        }
    }

    private RewardType GetRandomRewardType()
    {
        RewardType[] rewards =
        {
            RewardType.Augment,
            RewardType.Skill,
            RewardType.Item
        };

        return rewards[Random.Range(0, rewards.Length)];
    }

    private RewardType GetRandomRewardTypeExcept(RewardType except)
    {
        RewardType result = GetRandomRewardType();

        int safetyCount = 0;

        while (result == except && safetyCount < 20)
        {
            result = GetRandomRewardType();
            safetyCount++;
        }

        return result;
    }
}