using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class StageClear : MonoBehaviour
{
    public static StageClear Instance;
    
    [System.Serializable]
    private class RouteMarkerData
    {
        public RouteType routeType;
        public string displayName;
        public Sprite icon;
    }

    public enum RouteType
    {
        None,
        Shop,
        Augment,
        Skill,
        Item,
        Boss,
        NextStage
    }

    public enum RewardType
    {
        None,
        Augment,
        Skill,
        Item
    }

    private enum RoomType
    {
        Combat,
        Shop,
        Boss
    }

    [Header("전투 씬 이름 배열")]
    [SerializeField] private string[] combatSceneNames;

    [Header("상점 씬 이름")]
    [SerializeField] private string shopSceneName = "ShopStage";

    [Header("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("몬스터 태그")]
    [SerializeField] private string monsterTag = "Monster";

    [Header("몬스터 전멸 시 자동 클리어")]
    [SerializeField] private bool autoClearWhenNoMonster = true;

    [Header("씬 이동 딜레이")]
    [SerializeField] private float clearDelay = 1.0f;

    [Header("현재 스테이지 번호")]
    [SerializeField] private int currentStageNumber = 1;

    [Header("현재 맵 번호")]
    [SerializeField] private int currentMapNumber = 1;

    [Header("디버그")]
    [SerializeField] private bool isStageCleared;
    [SerializeField] private bool isLoadingNextScene;

    private RoomType currentRoomType = RoomType.Combat;
    private RewardType pendingRewardType = RewardType.None;
    [Header("출구 보상 마커 ")]
    [SerializeField] private GameObject routeMarkerSpritePrefab;

    [SerializeField] private RouteMarkerData[] routeMarkerDatas;

    private SceneMoveTriggerRelay[] routeSlots;

    private readonly List<RouteType> currentOpenedRoutes = new List<RouteType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        BindRouteSlots();
        ResetRoomState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindRouteSlots();
        ResetRoomState();

        if (currentRoomType == RoomType.Shop)
        {
            OnEnterShopScene();
        }
    }

    private void Update()
    {
        if (currentRoomType == RoomType.Shop) return;
        if (isStageCleared) return;
        if (isLoadingNextScene) return;
        if (!autoClearWhenNoMonster) return;

        GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);

        if (monsters.Length == 0)
        {
            ClearStage();
        }
    }

    private void BindRouteSlots()
    {
        routeSlots = FindObjectsByType<SceneMoveTriggerRelay>(FindObjectsSortMode.None);

        if (routeSlots == null || routeSlots.Length < 3)
        {
            Debug.LogWarning("ExitSlot 출구 오브젝트가 3개보다 적음");
        }

        LockAllRouteSlots();
    }

    private void ResetRoomState()
    {
        isStageCleared = false;
        isLoadingNextScene = false;

        currentOpenedRoutes.Clear();

        LockAllRouteSlots();
    }

    private void LockAllRouteSlots()
    {
        if (routeSlots == null) return;

        for (int i = 0; i < routeSlots.Length; i++)
        {
            if (routeSlots[i] == null) continue;

            routeSlots[i].SetRouteType(RouteType.None);
            routeSlots[i].SetRouteEnabled(false);

            // 출구 잠글 때 기존 마커 제거
            routeSlots[i].ClearRouteMarker();
        }
    }

    public void ClearStage()
    {
        if (isStageCleared) return;

        isStageCleared = true;

        Debug.Log($"맵 클리어 / 스테이지 : {currentStageNumber} / 맵 : {currentMapNumber}");

        GivePendingReward();

        HandleClearFlow();
    }

    private void HandleClearFlow()
    {
        // 1스테이지 1맵은 증강 고정
        if (currentStageNumber == 1 && currentMapNumber == 1)
        {
            Debug.Log("1스테이지 1맵 클리어 - 증강 고정 지급");

            GiveRewardImmediately(RewardType.Augment);

            // 증강 선택 후 출구 3개 생성
            OpenCombatClearRouteChoices();
            return;
        }

        // 6맵 클리어 후에는 선택지 없이 보스 출구만 생성
        if (currentMapNumber == 6)
        {
            Debug.Log("6맵 클리어 - 보스 출구 생성");

            OpenOnlyRoute(RouteType.Boss);
            return;
        }

        // 7맵은 보스
        if (currentMapNumber == 7)
        {
            Debug.Log("보스 클리어 - 다음 스테이지 출구 생성");

            GiveBossReward();
            OpenOnlyRoute(RouteType.NextStage);
            return;
        }

        // 일반 전투맵 클리어
        OpenCombatClearRouteChoices();
    }

    /// <summary>
    /// 전투맵 클리어 시:
    /// 상점 1개 고정 + 증강/스킬/아이템 중 2개 랜덤
    /// </summary>
    private void OpenCombatClearRouteChoices()
    {
        if (!HasEnoughRouteSlots()) return;

        currentOpenedRoutes.Clear();

        List<RouteType> selectedRoutes = new List<RouteType>();

        // 상점은 무조건 포함
        selectedRoutes.Add(RouteType.Shop);

        // 보상 3종 중 2개 랜덤, 중복 불가
        List<RouteType> rewardRoutes = new List<RouteType>
        {
            RouteType.Augment,
            RouteType.Skill,
            RouteType.Item
        };

        ShuffleRouteList(rewardRoutes);

        selectedRoutes.Add(rewardRoutes[0]);
        selectedRoutes.Add(rewardRoutes[1]);

        // 출구 위치도 랜덤
        ShuffleRouteList(selectedRoutes);

        ApplyRoutesToSlots(selectedRoutes);

        Debug.Log($"전투맵 출구 생성 : {selectedRoutes[0]} / {selectedRoutes[1]} / {selectedRoutes[2]}");
    }

    /// <summary>
    /// 상점맵 종료 시:
    /// 증강 / 스킬 / 아이템 3개를 랜덤 위치에 배정
    /// </summary>
    private void OpenShopClearRouteChoices()
    {
        if (!HasEnoughRouteSlots()) return;

        currentOpenedRoutes.Clear();

        // 만약 상점이 6번째 맵 위치라면,
        // 보스 전 긴장감 유지를 위해 보스만 열어주는 게 안전함.
        if (currentMapNumber >= 6)
        {
            Debug.Log("상점 종료 위치가 6맵 이상 - 보스 출구만 생성");
            OpenOnlyRoute(RouteType.Boss);
            return;
        }

        List<RouteType> selectedRoutes = new List<RouteType>
        {
            RouteType.Augment,
            RouteType.Skill,
            RouteType.Item
        };

        ShuffleRouteList(selectedRoutes);

        ApplyRoutesToSlots(selectedRoutes);

        Debug.Log($"상점맵 출구 생성 : {selectedRoutes[0]} / {selectedRoutes[1]} / {selectedRoutes[2]}");
    }

    /// <summary>
    /// 보스 / 다음 스테이지처럼 출구 하나만 열 때 사용
    /// </summary>
    private void OpenOnlyRoute(RouteType routeType)
    {
        if (routeSlots == null || routeSlots.Length == 0)
        {
            Debug.LogError("출구 슬롯이 없음");
            return;
        }

        currentOpenedRoutes.Clear();
        currentOpenedRoutes.Add(routeType);

        LockAllRouteSlots();

        int randomIndex = Random.Range(0, routeSlots.Length);

        routeSlots[randomIndex].SetRouteType(routeType);
        routeSlots[randomIndex].SetRouteEnabled(true);

        string displayName = GetRouteDisplayName(routeType);
        Sprite icon = GetRouteIcon(routeType);

        routeSlots[randomIndex].ShowSpriteRouteMarker(
            routeMarkerSpritePrefab,
            displayName,
            icon
        );

        Debug.Log($"단일 출구 생성 : {routeType}");
    }

    private void ApplyRoutesToSlots(List<RouteType> selectedRoutes)
    {
        LockAllRouteSlots();

        List<SceneMoveTriggerRelay> slotList = new List<SceneMoveTriggerRelay>();

        for (int i = 0; i < routeSlots.Length; i++)
        {
            if (routeSlots[i] == null) continue;
            slotList.Add(routeSlots[i]);
        }

        ShuffleSlotList(slotList);

        int count = Mathf.Min(3, selectedRoutes.Count, slotList.Count);

        for (int i = 0; i < count; i++)
        {
            RouteType routeType = selectedRoutes[i];

            currentOpenedRoutes.Add(routeType);

            slotList[i].SetRouteType(routeType);
            slotList[i].SetRouteEnabled(true);

            string displayName = GetRouteDisplayName(routeType);
            Sprite icon = GetRouteIcon(routeType);

            slotList[i].ShowSpriteRouteMarker(
                routeMarkerSpritePrefab,
                displayName,
                icon
            );
        }
    }

    private bool HasEnoughRouteSlots()
    {
        if (routeSlots == null || routeSlots.Length < 3)
        {
            Debug.LogError("출구 슬롯은 최소 3개 필요함: ExitSlot_1, ExitSlot_2, ExitSlot_3");
            return false;
        }

        return true;
    }

    private void ShuffleRouteList(List<RouteType> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            RouteType temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void ShuffleSlotList(List<SceneMoveTriggerRelay> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            SceneMoveTriggerRelay temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void TryMoveNextScene(Collider2D other, RouteType routeType)
    {
        if (other == null) return;
        if (!other.CompareTag(playerTag)) return;

        if (!isStageCleared) return;
        if (isLoadingNextScene) return;

        if (!currentOpenedRoutes.Contains(routeType))
        {
            Debug.LogWarning($"현재 열려있지 않은 경로 : {routeType}");
            return;
        }

        Debug.Log($"선택한 경로 : {routeType}");

        switch (routeType)
        {
            case RouteType.Shop:
                MoveToShopScene();
                break;

            case RouteType.Augment:
                pendingRewardType = RewardType.Augment;
                MoveToNextCombatMap();
                break;

            case RouteType.Skill:
                pendingRewardType = RewardType.Skill;
                MoveToNextCombatMap();
                break;

            case RouteType.Item:
                pendingRewardType = RewardType.Item;
                MoveToNextCombatMap();
                break;

            case RouteType.Boss:
                MoveToBossMap();
                break;

            case RouteType.NextStage:
                MoveToNextStage();
                break;
        }
    }

    private void MoveToShopScene()
    {
        if (string.IsNullOrEmpty(shopSceneName))
        {
            Debug.LogError("shopSceneName이 비어있음");
            return;
        }

        isLoadingNextScene = true;

        // 상점도 하나의 맵 위치를 사용함.
        // 예: 1-1 클리어 후 상점 선택
        // currentMapNumber 1 -> 2
        currentMapNumber++;

        currentRoomType = RoomType.Shop;

        StartCoroutine(LoadSceneRoutine(shopSceneName));
    }

    private void MoveToNextCombatMap()
    {
        currentMapNumber++;

        // 보상 선택 후 다음 위치가 7이면 보스맵으로 이동
        if (currentMapNumber >= 7)
        {
            MoveToBossMap();
            return;
        }

        string nextSceneName = GetCombatSceneNameByMapNumber(currentMapNumber);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError($"전투 씬을 찾지 못함 / currentMapNumber : {currentMapNumber}");
            return;
        }

        isLoadingNextScene = true;
        currentRoomType = RoomType.Combat;

        StartCoroutine(LoadSceneRoutine(nextSceneName));
    }

    private void MoveToBossMap()
    {
        currentMapNumber = 7;

        string bossSceneName = GetCombatSceneNameByMapNumber(currentMapNumber);

        if (string.IsNullOrEmpty(bossSceneName))
        {
            Debug.LogError("보스 씬을 찾지 못함");
            return;
        }

        isLoadingNextScene = true;
        currentRoomType = RoomType.Boss;

        StartCoroutine(LoadSceneRoutine(bossSceneName));
    }

    private void MoveToNextStage()
    {
        Debug.Log("다음 스테이지 이동");

        currentStageNumber++;
        currentMapNumber = 1;
        pendingRewardType = RewardType.None;
        currentRoomType = RoomType.Combat;

        string firstSceneName = GetCombatSceneNameByMapNumber(currentMapNumber);

        if (string.IsNullOrEmpty(firstSceneName))
        {
            Debug.LogError("다음 스테이지 첫 씬 이름이 없음");
            return;
        }

        isLoadingNextScene = true;

        StartCoroutine(LoadSceneRoutine(firstSceneName));
    }

    private string GetCombatSceneNameByMapNumber(int mapNumber)
    {
        if (combatSceneNames == null || combatSceneNames.Length == 0)
            return null;

        // currentMapNumber는 1부터 시작.
        // 배열 index는 0부터 시작.
        // 그래서 mapNumber - 1 사용.
        int index = mapNumber - 1;

        if (index < 0 || index >= combatSceneNames.Length)
            return null;

        return combatSceneNames[index];
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Time.timeScale = 1f;

        yield return new WaitForSeconds(clearDelay);

        SceneManager.LoadScene(sceneName);
    }

    private void GivePendingReward()
    {
        
        if (pendingRewardType == RewardType.None)
            return;

        Debug.Log($"예약 보상 지급 : {pendingRewardType}");

        GiveRewardImmediately(pendingRewardType);

        pendingRewardType = RewardType.None;
    }
    
    private void GiveRewardImmediately(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.Augment:
                if (AugUIManager.instance != null)
                {
                    AugUIManager.instance.ShowAugmentation();
                }
                break;

            case RewardType.Skill:
                if (SkillSelectUIManager.Instance != null)
                {
                    SkillSelectUIManager.Instance.IsSkillOpened = true;
                }
                break;

            case RewardType.Item:
                
                if (NewItemUIManager.Instance != null)
                {
                    NewItemUIManager.Instance.IsOpenedItem = true;
                }
                break;
        }
    }

    private void GiveBossReward()
    {
        float rand = Random.Range(0f, 100f);

        if (rand < 50f)
        {
            Debug.Log("보스 보상 : 증강");
            GiveRewardImmediately(RewardType.Augment);
        }
        else
        {
            Debug.Log("보스 보상 : 스킬");
            GiveRewardImmediately(RewardType.Skill);
        }
    }

    private void OnEnterShopScene()
    {
        Debug.Log("상점 씬 입장");

        if (NewItemUIManager.Instance != null)
        {
            NewItemUIManager.Instance.IsOpenedItem = true;
        }
    }

    public void CompleteShop()
    {
        if (currentRoomType != RoomType.Shop)
            return;

        if (isStageCleared) return;

        Debug.Log("상점 종료 - 보상 출구 3개 생성");

        isStageCleared = true;

        OpenShopClearRouteChoices();
    }
    
    private RouteMarkerData GetRouteMarkerData(RouteType routeType)
    {
        if (routeMarkerDatas == null)
            return null;

        for (int i = 0; i < routeMarkerDatas.Length; i++)
        {
            if (routeMarkerDatas[i] == null) continue;

            if (routeMarkerDatas[i].routeType == routeType)
                return routeMarkerDatas[i];
        }

        return null;
    }

    private string GetRouteDisplayName(RouteType routeType)
    {
        RouteMarkerData data = GetRouteMarkerData(routeType);

        if (data != null && !string.IsNullOrEmpty(data.displayName))
            return data.displayName;

        return routeType.ToString();
    }

    private Sprite GetRouteIcon(RouteType routeType)
    {
        RouteMarkerData data = GetRouteMarkerData(routeType);

        if (data != null)
            return data.icon;

        return null;
    }
    

    public void ResetRun()
    {
        currentStageNumber = 1;
        currentMapNumber = 1;
        currentRoomType = RoomType.Combat;
        pendingRewardType = RewardType.None;

        isStageCleared = false;
        isLoadingNextScene = false;

        Time.timeScale = 1f;

        string firstSceneName = GetCombatSceneNameByMapNumber(1);

        if (!string.IsNullOrEmpty(firstSceneName))
        {
            SceneManager.LoadScene(firstSceneName);
        }
    }

    
}