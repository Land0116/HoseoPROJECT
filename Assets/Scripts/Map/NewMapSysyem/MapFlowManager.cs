using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class CombatSceneData
{
    [Header("전투 씬 이름")]
    public string sceneName;

    [Header("이 전투 씬이 가지고 있는 입구 그룹")]
    public GateGroup[] entranceGroups;

    public bool HasEntranceGroup(GateGroup group)
    {
        if (entranceGroups == null) return false;

        for (int i = 0; i < entranceGroups.Length; i++)
        {
            if (entranceGroups[i] == group)
                return true;
        }

        return false;
    }
}

public class MapFlowManager : MonoBehaviour
{
    public static MapFlowManager Instance { get; private set; }

    [Header("현재 액트")]
    [SerializeField] private int currentAct = 1;

    [Header("현재 액트의 전투방 번호")]
    [SerializeField] private int currentCombatRoomNumber = 1;

    [Header("보스 진입 전 마지막 전투방 번호")]
    [SerializeField] private int bossEnterCombatRoomNumber = 6;

    [Header("다음 방에서 필요한 입구 그룹")]
    [SerializeField] private GateGroup requiredEntranceGroup = GateGroup.A;

    [Header("다음 전투방 클리어 후 지급할 보상")]
    [SerializeField] private RewardType pendingRewardType = RewardType.None;

    [Header("현재 방 종류")]
    [SerializeField] private RoomKind currentRoomKind = RoomKind.Combat;

    [Header("전투 씬 목록")]
    [SerializeField] private CombatSceneData[] combatScenes = new CombatSceneData[4];

    [Header("씬 이름")]
    [SerializeField] private string shopSceneName = "ShopRoom";
    [SerializeField] private string bossSceneName = "BossRoom";

    private string lastCombatSceneName;

    public int CurrentAct => currentAct;
    public int CurrentCombatRoomNumber => currentCombatRoomNumber;
    public GateGroup RequiredEntranceGroup => requiredEntranceGroup;
    public RewardType PendingRewardType => pendingRewardType;
    public RoomKind CurrentRoomKind => currentRoomKind;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public bool IsFirstCombatRoomOfAct()
    {
        return currentCombatRoomNumber == 1;
    }

    public bool IsLastCombatRoomBeforeBoss()
    {
        return currentCombatRoomNumber >= bossEnterCombatRoomNumber;
        
    }
    
    private void LoadRandomCombatScene()
    {
        string sceneName = PickRandomCombatSceneName(requiredEntranceGroup);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[MapFlowManager] {requiredEntranceGroup} 입구를 가진 전투 씬이 없음");
            return;
        }

        lastCombatSceneName = sceneName;
        SceneManager.LoadScene(sceneName);
    }
    private string PickRandomCombatSceneName(GateGroup requiredGroup)
    {
        if (combatScenes == null || combatScenes.Length == 0)
        {
            Debug.LogError("[MapFlowManager] combatScenes 배열이 비어 있음");
            return null;
        }

        // 후보를 임시 배열처럼 쓰기 위한 리스트
        System.Collections.Generic.List<CombatSceneData> candidates =
            new System.Collections.Generic.List<CombatSceneData>();

        for (int i = 0; i < combatScenes.Length; i++)
        {
            CombatSceneData data = combatScenes[i];

            if (data == null) continue;
            if (string.IsNullOrWhiteSpace(data.sceneName)) continue;

            // 필요한 입구 그룹이 있는 씬만 후보에 넣음
            if (!data.HasEntranceGroup(requiredGroup)) continue;

            candidates.Add(data);
        }

        if (candidates.Count <= 0)
        {
            return null;
        }

        // 후보가 2개 이상이면 직전 전투 씬은 최대한 피함
        if (candidates.Count > 1 && !string.IsNullOrEmpty(lastCombatSceneName))
        {
            candidates.RemoveAll(data => data.sceneName == lastCombatSceneName);

            // 전부 제거되어버렸으면 다시 전체 후보 사용
            if (candidates.Count <= 0)
            {
                for (int i = 0; i < combatScenes.Length; i++)
                {
                    CombatSceneData data = combatScenes[i];

                    if (data == null) continue;
                    if (string.IsNullOrWhiteSpace(data.sceneName)) continue;
                    if (!data.HasEntranceGroup(requiredGroup)) continue;

                    candidates.Add(data);
                }
            }
        }

        CombatSceneData picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        return picked.sceneName;
    }

    public RewardType GetCurrentCombatRewardType()
    {
        // 1-1, 2-1, 3-1은 무조건 증강 보상
        if (IsFirstCombatRoomOfAct())
        {
            return RewardType.Augment;
        }

        // 일반 전투방은 이전 출구에서 선택한 보상 사용
        return pendingRewardType;
    }

    public void EnterNextRoomFromGate(GateGroup usedGateGroup, RoomKind nextRoomKind, RewardType selectedRewardType)
    {
        // 다음 방에서 플레이어가 나와야 하는 입구 그룹 계산
        requiredEntranceGroup = GetOppositeGateGroup(usedGateGroup);

        // 핵심 수정:
        // 전투방, 상점방, 보스방으로 이동할 때 모두 현재 액트 진행 번호를 증가시킨다.
        //
        // 예:
        // 전투방 -> 상점 -> 전투방
        // 1      -> 2    -> 3
        //
        // 전투방 -> 상점 -> 보스방
        // 5      -> 6    -> 7
        currentCombatRoomNumber++;

        if (nextRoomKind == RoomKind.Shop)
        {
            currentRoomKind = RoomKind.Shop;

            // 상점방 자체는 전투 클리어 보상이 아니므로 None
            pendingRewardType = RewardType.None;

            SceneManager.LoadScene(shopSceneName);
            return;
        }

        if (nextRoomKind == RoomKind.Boss)
        {
            currentRoomKind = RoomKind.Boss;
            pendingRewardType = RewardType.None;

            SceneManager.LoadScene(bossSceneName);
            return;
        }

        if (nextRoomKind == RoomKind.Combat)
        {
            currentRoomKind = RoomKind.Combat;

            // 상점방 또는 전투방 출구에서 선택한 보상.
            // 다음 전투방 클리어 후 이 보상이 생성된다.
            pendingRewardType = selectedRewardType;

            LoadRandomCombatScene();
        }
    }

    public void CompleteBossAndGoNextAct(GateGroup usedGateGroup)
    {
        requiredEntranceGroup = GetOppositeGateGroup(usedGateGroup);

        currentAct++;
        currentCombatRoomNumber = 1;
        currentRoomKind = RoomKind.Combat;
        pendingRewardType = RewardType.None;

        LoadRandomCombatScene();
    }

    public bool ShouldShopConnectToBoss()
    {
        return currentCombatRoomNumber >= bossEnterCombatRoomNumber;
    }

    private GateGroup GetOppositeGateGroup(GateGroup gateGroup)
    {
        switch (gateGroup)
        {
            case GateGroup.A:
                return GateGroup.B;

            case GateGroup.B:
                return GateGroup.A;

            case GateGroup.C:
                return GateGroup.D;

            case GateGroup.D:
                return GateGroup.C;

            default:
                return GateGroup.A;
        }
    }

    [ContextMenu("Reset Flow")]
    private void ResetFlow()
    {
        ResetFlowStateOnly();
    }
    
    public void StartNewRun()
    {
        ResetFlowStateOnly();
        LoadFirstRandomCombatScene();
    }

    public void ResetFlowStateOnly()
    {
        currentAct = 1;
        currentCombatRoomNumber = 1;
        requiredEntranceGroup = GateGroup.A;
        pendingRewardType = RewardType.None;
        currentRoomKind = RoomKind.Combat;
        lastCombatSceneName = null;
    }

    private void LoadFirstRandomCombatScene()
    {
        CombatSceneData pickedScene = PickRandomAnyCombatScene();

        if (pickedScene == null)
        {
            Debug.LogError("[MapFlowManager] 시작 가능한 전투 씬이 없음");
            return;
        }

        if (pickedScene.entranceGroups != null && pickedScene.entranceGroups.Length > 0)
        {
            requiredEntranceGroup = pickedScene.entranceGroups[
                UnityEngine.Random.Range(0, pickedScene.entranceGroups.Length)
            ];
        }
        else
        {
            Debug.LogWarning($"[MapFlowManager] {pickedScene.sceneName}에 EntranceGroups가 없음. A로 시작 처리");
            requiredEntranceGroup = GateGroup.A;
        }

        currentRoomKind = RoomKind.Combat;
        pendingRewardType = RewardType.None;
        currentCombatRoomNumber = 1;
        lastCombatSceneName = pickedScene.sceneName;

        SceneManager.LoadScene(pickedScene.sceneName);
    }

    private CombatSceneData PickRandomAnyCombatScene()
    {
        if (combatScenes == null || combatScenes.Length == 0)
        {
            Debug.LogError("[MapFlowManager] combatScenes 배열이 비어 있음");
            return null;
        }

        System.Collections.Generic.List<CombatSceneData> candidates =
            new System.Collections.Generic.List<CombatSceneData>();

        for (int i = 0; i < combatScenes.Length; i++)
        {
            CombatSceneData data = combatScenes[i];

            if (data == null) continue;
            if (string.IsNullOrWhiteSpace(data.sceneName)) continue;

            candidates.Add(data);
        }

        if (candidates.Count <= 0)
        {
            Debug.LogError("[MapFlowManager] 유효한 전투 씬 데이터가 없음");
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
    
}