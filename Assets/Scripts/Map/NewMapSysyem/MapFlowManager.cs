using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
[Serializable]
public class ActSceneSet
{
    [Header("액트 번호")]
    public int actNumber = 1;

    [Header("해당 액트의 전투 씬 목록")]
    public CombatSceneData[] combatScenes = new CombatSceneData[4];

    [Header("해당 액트의 상점 씬")]
    public string shopSceneName;

    [Header("해당 액트의 보스 씬")]
    public string bossSceneName;
}

public class MapFlowManager : MonoBehaviour
{
    public static MapFlowManager Instance { get; private set; }
    [Header("액트별 씬 세트")]
    [SerializeField] private ActSceneSet[] actSceneSets = new ActSceneSet[3];
    [Header("게임 클리어 씬")]
    [SerializeField] private string gameClearSceneName = "GameClear";

    [Header("현재 액트")] [SerializeField] private int currentAct = 1;
    [Header("액트 진행")] [SerializeField] private int maxAct = 3;

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
        ActSceneSet actSet = GetCurrentActSceneSet();

        if (actSet == null) return;

        string sceneName = PickRandomCombatSceneName(
            actSet.combatScenes,
            requiredEntranceGroup
        );

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[MapFlowManager] Act {currentAct}에서 {requiredEntranceGroup} 입구를 가진 전투 씬이 없음");
            return;
        }

        lastCombatSceneName = sceneName;
        LoadSceneByTransition(sceneName, true);
    }
    private string PickRandomCombatSceneName(CombatSceneData[] sceneList, GateGroup requiredGroup)
    {
        if (sceneList == null || sceneList.Length == 0)
        {
            Debug.LogError($"[MapFlowManager] Act {currentAct} combatScenes 배열이 비어 있음");
            return null;
        }

        System.Collections.Generic.List<CombatSceneData> candidates =
            new System.Collections.Generic.List<CombatSceneData>();

        for (int i = 0; i < sceneList.Length; i++)
        {
            CombatSceneData data = sceneList[i];

            if (data == null) continue;
            if (string.IsNullOrWhiteSpace(data.sceneName)) continue;
            if (!data.HasEntranceGroup(requiredGroup)) continue;

            candidates.Add(data);
        }

        if (candidates.Count <= 0)
        {
            return null;
        }

        if (candidates.Count > 1 && !string.IsNullOrEmpty(lastCombatSceneName))
        {
            candidates.RemoveAll(data => data.sceneName == lastCombatSceneName);

            if (candidates.Count <= 0)
            {
                for (int i = 0; i < sceneList.Length; i++)
                {
                    CombatSceneData data = sceneList[i];

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
        requiredEntranceGroup = GetOppositeGateGroup(usedGateGroup);

        Debug.Log(
            $"[MapFlow] Act={currentAct}, RoomNumber={currentCombatRoomNumber}, " +
            $"UsedGate={usedGateGroup}, RequiredEntrance={requiredEntranceGroup}, " +
            $"NextRoom={nextRoomKind}, Reward={selectedRewardType}"
        );

        currentCombatRoomNumber++;

        ActSceneSet actSet = GetCurrentActSceneSet();

        if (actSet == null) return;

        if (nextRoomKind == RoomKind.Shop)
        {
            currentRoomKind = RoomKind.Shop;
            pendingRewardType = RewardType.None;

            LoadSceneByTransition(actSet.shopSceneName, true);
            return;
        }

        if (nextRoomKind == RoomKind.Boss)
        {
            currentRoomKind = RoomKind.Boss;
            pendingRewardType = RewardType.None;

            LoadSceneByTransition(actSet.bossSceneName, false);
            return;
        }

        if (nextRoomKind == RoomKind.Combat)
        {
            currentRoomKind = RoomKind.Combat;
            pendingRewardType = selectedRewardType;

            LoadRandomCombatScene();
        }
    }

    public void CompleteBossAndGoNextAct()
    {
        currentAct++;

        if (currentAct > maxAct)
        {
            currentRoomKind = RoomKind.Combat;
            pendingRewardType = RewardType.None;
            currentCombatRoomNumber = 1;
            lastCombatSceneName = null;

            LoadSceneByTransition(gameClearSceneName, true);
            return;
        }

        currentCombatRoomNumber = 1;
        currentRoomKind = RoomKind.Combat;
        pendingRewardType = RewardType.None;
        lastCombatSceneName = null;

        LoadRandomCombatScene();
    }

    public void CompleteBossAndGoNextAct(GateGroup usedGateGroup)
    {
        requiredEntranceGroup = GetOppositeGateGroup(usedGateGroup);

        CompleteBossAndGoNextAct();
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
                Random.Range(0, pickedScene.entranceGroups.Length)
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

        LoadSceneByTransition(pickedScene.sceneName, true);
    }

    private CombatSceneData PickRandomAnyCombatScene()
    {
        ActSceneSet actSet = GetCurrentActSceneSet();

        if (actSet == null) return null;

        CombatSceneData[] sceneList = actSet.combatScenes;

        if (sceneList == null || sceneList.Length == 0)
        {
            Debug.LogError($"[MapFlowManager] Act {currentAct} combatScenes 배열이 비어 있음");
            return null;
        }

        System.Collections.Generic.List<CombatSceneData> candidates =
            new System.Collections.Generic.List<CombatSceneData>();

        for (int i = 0; i < sceneList.Length; i++)
        {
            CombatSceneData data = sceneList[i];

            if (data == null) continue;
            if (string.IsNullOrWhiteSpace(data.sceneName)) continue;

            candidates.Add(data);
        }

        if (candidates.Count <= 0)
        {
            Debug.LogError($"[MapFlowManager] Act {currentAct}에 유효한 전투 씬 데이터가 없음");
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
    private void LoadSceneByTransition(string sceneName, bool fadeFromBlackAfterLoad = true)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[MapFlowManager] 로드할 씬 이름이 비어 있음");
            return;
        }

        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.LoadSceneWithFade(sceneName, fadeFromBlackAfterLoad);
            return;
        }
        
        SceneManager.LoadScene(sceneName);
        
    }
    private ActSceneSet GetCurrentActSceneSet()
    {
        if (actSceneSets == null || actSceneSets.Length == 0)
        {
            Debug.LogError("[MapFlowManager] actSceneSets가 비어 있음");
            return null;
        }

        for (int i = 0; i < actSceneSets.Length; i++)
        {
            ActSceneSet set = actSceneSets[i];

            if (set == null) continue;

            if (set.actNumber == currentAct)
            {
                return set;
            }
        }

        Debug.LogError($"[MapFlowManager] currentAct {currentAct}에 해당하는 ActSceneSet이 없음");
        return null;
    }
}