using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class StageClear : MonoBehaviour
{
    public static StageClear Instance;
    public static System.Action<int, bool> OnStageChanged; //* 0524
    [System.Serializable]
    private class RouteMarkerData
    {
        public RouteType routeType;
        public string displayName;
        public Sprite icon;
    }
    [System.Serializable]
    private class RewardIconData
    {
        public RewardType rewardType;
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
        NextStage,
        Exit
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

    [System.Serializable]
    private class StageSceneData
    {
        [Header("스테이지 번호")] public int stageNumber = 1;

        [Header("전투 씬 이름 배열 1~7")] public string[] combatSceneNames = new string[7];

        [Header("상점 씬 이름")] public string shopSceneName;
    }

    [Header("스테이지별 씬 데이터")] [SerializeField]
    private StageSceneData[] stageSceneDatas;

    [Header("메인 씬 이름")] [SerializeField] private string mainSceneName = "Main";

    [Header("플레이어 태그")] [SerializeField] private string playerTag = "Player";

    [Header("몬스터 태그")] [SerializeField] private string monsterTag = "Monster";

    [Header("몬스터 전멸 시 자동 클리어")] [SerializeField]
    private bool autoClearWhenNoMonster = true;

    [Header("씬 이동 딜레이")] [SerializeField] private float clearDelay = 1.0f;

    [Header("현재 스테이지 번호")] [SerializeField]
    private int currentStageNumber = 1;

    [Header("현재 맵 번호")] [SerializeField] private int currentMapNumber = 1;

    [Header("최종 스테이지 번호")] [SerializeField]
    private int finalStageNumber = 3;

    [Header("몬스터 웨이브")] [SerializeField] private int combatWaveCount = 2;

    [SerializeField] private float nextWaveDelay = 0.5f;

    private MonsterSpawner[] monsterSpawners;
    private int currentWaveIndex = 0;
    private bool hasStartedWaveInRoom = false;
    private bool isWaveChanging = false;
    private Coroutine waveCoroutine;


    [Header("디버그")] [SerializeField] private bool isStageCleared;
    [SerializeField] private bool isLoadingNextScene;

    private RoomType currentRoomType = RoomType.Combat;
    private RewardType pendingRewardType = RewardType.None;
    [Header("출구 보상 마커 ")] 
    [SerializeField] private GameObject routeMarkerSpritePrefab;
    [SerializeField] private RouteMarkerData[] routeMarkerDatas;

    [Header("플레이어 위치에 생성되는 보상 프리팹")]
    [SerializeField] private GameObject rewardInteractPrefab;
    [SerializeField] private float rewardSpawnYOffset = 0.7f;
    [SerializeField] private RewardIconData[] rewardIconDatas;
    private GameObject currentRewardObject;
    
    private SceneMoveTriggerRelay[] routeSlots;
    private readonly List<RouteType> currentOpenedRoutes = new List<RouteType>();

    private void Awake()
    {
        Debug.Log("StageClear Awake 실행됨");
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
        if (IsMainScene())
        {
            ResetRunStateOnly();
            return;
        }

        SyncCurrentRoomByLoadedScene(SceneManager.GetActiveScene().name);

        BindRouteSlots();
        ResetRoomState();

        if (currentRoomType != RoomType.Shop)
        {
            StartRoomWavesIfNeeded();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainSceneName)
        {
            ResetRunStateOnly();

            Debug.Log("메인씬 로드 - 런 상태 초기화 / 출구 시스템 비활성화");
            return;
        }

        SyncCurrentRoomByLoadedScene(scene.name);

        // 보스룸에 들어온 순간 이전 보상 예약은 무조건 제거
        if (currentRoomType == RoomType.Boss)
        {
            pendingRewardType = RewardType.None;

            Debug.Log("보스룸 입장 - 이전 선택 보상 예약 제거");
        }

        BindRouteSlots();
        ResetRoomState();

        if (currentRoomType == RoomType.Shop)
        {
            OnEnterShopScene();
        }
        else
        {
            StartRoomWavesIfNeeded();
        }

        if (OnStageChanged != null)
        {
            OnStageChanged.Invoke(currentStageNumber, IsBossRoom());
        }//* 0524
    }

    private void SyncCurrentRoomByLoadedScene(string sceneName)
    {
        if (TryGetCombatSceneInfo(sceneName, out int stageNumber, out int mapNumber))
        {
            currentStageNumber = stageNumber;
            currentMapNumber = mapNumber;

            currentRoomType = mapNumber == 7
                ? RoomType.Boss
                : RoomType.Combat;

            return;
        }

        if (TryGetShopStageNumber(sceneName, out int shopStageNumber))
        {
            currentStageNumber = shopStageNumber;
            currentRoomType = RoomType.Shop;
        }
    }

    private void Update()
    {
        if (IsMainScene()) return;
        if (currentRoomType == RoomType.Shop) return;
        if (isStageCleared) return;
        if (isLoadingNextScene) return;
        if (!autoClearWhenNoMonster) return;

        GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);

        if (monsters.Length == 0)
        {
            HandleAllMonstersDefeated();
        }
    }

    private void StartRoomWavesIfNeeded()
    {
        if (IsMainScene()) return;
        if (currentRoomType == RoomType.Shop) return;
        if (isStageCleared) return;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        waveCoroutine = StartCoroutine(StartRoomWavesRoutine());
    }

    private IEnumerator StartRoomWavesRoutine()
    {
        // 씬 로드 직후 MonsterSpawner들이 준비될 시간 확보
        yield return null;

        BindMonsterSpawners();

        currentWaveIndex = 0;
        hasStartedWaveInRoom = true;

        yield return SpawnNextWaveRoutine();

        waveCoroutine = null;
    }

    private void BindMonsterSpawners()
    {
        monsterSpawners = FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None);

        if (monsterSpawners == null || monsterSpawners.Length == 0)
        {
            Debug.LogWarning("현재 방에 MonsterSpawner가 없음");
        }
    }

    private int GetCurrentRoomWaveCount()
    {
        // 보스방은 2웨이브 필요 없음.
        if (currentRoomType == RoomType.Boss || currentMapNumber == 7)
            return 1;

        return Mathf.Max(1, combatWaveCount);
    }

    private void HandleAllMonstersDefeated()
    {
        if (!hasStartedWaveInRoom) return;
        if (isWaveChanging) return;
        if (isStageCleared) return;

        int maxWaveCount = GetCurrentRoomWaveCount();

        // 아직 남은 웨이브가 있으면 다음 웨이브 생성
        if (currentWaveIndex < maxWaveCount)
        {
            if (waveCoroutine == null)
            {
                waveCoroutine = StartCoroutine(SpawnNextWaveAfterDelay());
            }

            return;
        }

        // 모든 웨이브 처치 후에만 클리어 처리
        ClearStage();
    }

    private IEnumerator SpawnNextWaveAfterDelay()
    {
        isWaveChanging = true;

        if (nextWaveDelay > 0f)
            yield return new WaitForSeconds(nextWaveDelay);

        yield return SpawnNextWaveRoutine();

        waveCoroutine = null;
    }

    private IEnumerator SpawnNextWaveRoutine()
    {
        isWaveChanging = true;

        if (monsterSpawners == null || monsterSpawners.Length == 0)
        {
            BindMonsterSpawners();
        }

        if (monsterSpawners == null || monsterSpawners.Length == 0)
        {
            Debug.LogWarning("MonsterSpawner가 없어서 웨이브 생성 불가");
            isWaveChanging = false;
            yield break;
        }

        currentWaveIndex++;

        Debug.Log($"웨이브 생성 : {currentWaveIndex} / {GetCurrentRoomWaveCount()}");

        for (int i = 0; i < monsterSpawners.Length; i++)
        {
            if (monsterSpawners[i] == null) continue;

            monsterSpawners[i].SetSpawn(true);
        }

        // 생성 직후 바로 몬스터 0마리 체크되는 것 방지
        yield return null;
        yield return null;

        isWaveChanging = false;
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

        currentWaveIndex = 0;
        hasStartedWaveInRoom = false;
        isWaveChanging = false;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        if (currentRewardObject != null)
        {
            Destroy(currentRewardObject);
            currentRewardObject = null;
        }

        monsterSpawners = null;

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

    private bool IsBossSceneName(string sceneName)
    {
        return TryGetCombatSceneInfo(sceneName, out int stageNumber, out int mapNumber)
               && mapNumber == 7;
    }

    private bool TryGetCombatSceneInfo(string sceneName, out int stageNumber, out int mapNumber)
    {
        stageNumber = 1;
        mapNumber = 1;

        if (stageSceneDatas == null)
            return false;

        for (int i = 0; i < stageSceneDatas.Length; i++)
        {
            StageSceneData data = stageSceneDatas[i];
            if (data == null) continue;
            if (data.combatSceneNames == null) continue;

            int realStageNumber = data.stageNumber > 0 ? data.stageNumber : i + 1;

            for (int j = 0; j < data.combatSceneNames.Length; j++)
            {
                if (sceneName == data.combatSceneNames[j])
                {
                    stageNumber = realStageNumber;
                    mapNumber = j + 1;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetShopStageNumber(string sceneName, out int stageNumber)
    {
        stageNumber = 1;

        if (stageSceneDatas == null)
            return false;

        for (int i = 0; i < stageSceneDatas.Length; i++)
        {
            StageSceneData data = stageSceneDatas[i];
            if (data == null) continue;

            if (!string.IsNullOrEmpty(data.shopSceneName) &&
                sceneName == data.shopSceneName)
            {
                stageNumber = data.stageNumber > 0 ? data.stageNumber : i + 1;
                return true;
            }
        }

        return false;
    }

    private bool IsBossRoom()
    {
        return currentRoomType == RoomType.Boss || currentMapNumber == 7;
    }

    private bool IsFinalStageBossCleared()
    {
        return currentStageNumber >= finalStageNumber && currentMapNumber == 7;
    }

    public void ClearStage()
    {
        if (IsMainScene()) return;
        if (isStageCleared) return;

        isStageCleared = true;

        Debug.Log($"맵 클리어 / 스테이지 : {currentStageNumber} / 맵 : {currentMapNumber}");

        if (currentMapNumber == 7)
        {
            pendingRewardType = RewardType.None;

            if (IsFinalStageBossCleared())
            {
                Debug.Log("최종 보스 클리어 - 탈출 출구 생성");

                OpenOnlyRoute(RouteType.Exit);
                return;
            }

            Debug.Log("보스 클리어 - 다음 스테이지 출구 생성");

            OpenOnlyRoute(RouteType.NextStage);
            return;
        }

        GivePendingReward();

        HandleClearFlow();
    }

    private void HandleClearFlow()
    {
        if (currentMapNumber == 1)
        {
            Debug.Log($"{currentStageNumber}스테이지 1맵 클리어 - 증강 고정 보상 오브젝트 생성");

            SpawnRewardObjectAtPlayer(RewardType.Augment);

            OpenCombatClearRouteChoices();
            return;
        }

        // 6맵 클리어 후에는 선택지 없이 보스 출구만 생성
        if (currentMapNumber == 6)
        {
            Debug.Log("6맵 클리어 - 보스 출구 생성");

            pendingRewardType = RewardType.None;
            OpenOnlyRoute(RouteType.Boss);
            return;
        }

        if (currentMapNumber == 7)
        {
            if (IsFinalStageBossCleared())
            {
                Debug.Log("최종 보스 클리어 - 탈출 출구 생성");

                OpenOnlyRoute(RouteType.Exit);
                return;
            }

            Debug.Log("보스 클리어 - 다음 스테이지 출구 생성");

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
        if (IsMainScene()) return;
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
    /// 출구 3개를 생성한다.
    /// 
    /// 핵심 규칙:
    /// 1. 상점 안에서는 상점 출구가 다시 나오면 안 된다.
    /// 2. 상점 안에서는 보스 / 다음 스테이지 출구도 직접 표시하지 않는다.
    /// 3. 단, 상점이 6번째 위치라면 보스 출구만 표시한다.
    /// 4. 증강 / 스킬 / 아이템 3개만 출구에 표시한다.
    /// 5. 상점도 하나의 맵 위치를 차지하므로,
    ///    이 출구를 선택하면 MoveToNextCombatMap()에서 다음 위치로 이동한다.
    /// </summary>
    private void OpenShopClearRouteChoices()
    {
        if (IsMainScene()) return;

        // 상점이 6번째 위치라면 보상 선택 없이 보스룸으로 가야 함
        if (currentMapNumber >= 6)
        {
            Debug.Log("6번째 위치 상점 종료 - 보스 출구 전체 생성");

            pendingRewardType = RewardType.None;

            OpenSameRouteOnAllSlots(RouteType.Boss);
            return;
        }

        if (!HasEnoughRouteSlots()) return;

        currentOpenedRoutes.Clear();

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
    
    private void OpenSameRouteOnAllSlots(RouteType routeType)
    {
        if (IsMainScene()) return;

        if (routeSlots == null || routeSlots.Length == 0)
        {
            Debug.LogError("출구 슬롯이 없음");
            return;
        }

        currentOpenedRoutes.Clear();
        currentOpenedRoutes.Add(routeType);

        LockAllRouteSlots();

        string displayName = GetRouteDisplayName(routeType);
        Sprite icon = GetRouteIcon(routeType);

        for (int i = 0; i < routeSlots.Length; i++)
        {
            if (routeSlots[i] == null) continue;

            routeSlots[i].SetRouteType(routeType);
            routeSlots[i].SetRouteEnabled(true);

            routeSlots[i].ShowSpriteRouteMarker(
                routeMarkerSpritePrefab,
                displayName,
                icon
            );
        }

        Debug.Log($"동일 경로 출구 전체 생성 : {routeType}");
    }

    /// <summary>
    /// 보스 / 다음 스테이지처럼 출구 하나만 열 때 사용
    /// </summary>
    private void OpenOnlyRoute(RouteType routeType)
    {
        if (IsMainScene()) return;
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
        if (IsMainScene()) return;
        if (other == null) return;
        if (!other.CompareTag(playerTag)) return;

        if (!isStageCleared) return;
        if (isLoadingNextScene) return;
        
        if (currentRewardObject != null)
        {
            Debug.LogWarning("아직 획득하지 않은 보상이 있음. 보상을 먼저 획득해야 출구를 이용할 수 있음");
            return;
        }

        if (!currentOpenedRoutes.Contains(routeType))
        {
            Debug.LogWarning($"현재 열려있지 않은 경로 : {routeType}");
            return;
        }

        // 상점맵 내부 출구 제한
        // 기본적으로 상점 안에서는 Shop / NextStage 출구를 막는다.
        // 단, currentMapNumber가 6 이상이면 보스 출구는 허용한다.
        // 이유: 1-6 위치가 상점맵일 경우, 다음은 보스방으로 가야 하기 때문.
        if (currentRoomType == RoomType.Shop)
        {
            if (routeType == RouteType.Shop ||
                routeType == RouteType.NextStage)
            {
                Debug.LogWarning($"상점맵에서는 사용할 수 없는 출구 타입 : {routeType}");
                return;
            }

            if (routeType == RouteType.Boss && currentMapNumber < 6)
            {
                Debug.LogWarning($"아직 보스방으로 갈 수 없는 상점 위치임 / currentMapNumber : {currentMapNumber}");
                return;
            }
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
                pendingRewardType = RewardType.None;
                MoveToBossMap();
                break;

            case RouteType.NextStage:
                MoveToNextStage();
                break;
            case RouteType.Exit:
                CompleteGame();
                break;
        }
    }

    private void CompleteGame()
    {
        if (isLoadingNextScene) return;

        Debug.Log("게임 최종 클리어");

        // OnTriggerStay로 반복 호출되는 것 방지
        isLoadingNextScene = true;

        // StageClear는 UI를 직접 켜지 않는다.
        // 클리어 UI 출력은 PlayerUIManager에게 요청만 한다.
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowGameClearUI();
        }
        else
        {
            Debug.LogWarning("PlayerUIManager.Instance가 없어서 게임 클리어 UI를 열 수 없음");
        }
    }

    private void MoveToShopScene()
    {
        string currentShopSceneName = GetCurrentShopSceneName();

        if (string.IsNullOrEmpty(currentShopSceneName))
        {
            Debug.LogError($"상점 씬 이름이 비어있음 / currentStageNumber : {currentStageNumber}");
            return;
        }

        isLoadingNextScene = true;

        // 상점도 하나의 맵 위치를 사용함.
        // 예: 1-1 클리어 후 상점 선택
        // currentMapNumber 1 -> 2
        currentMapNumber++;

        currentRoomType = RoomType.Shop;

        StartCoroutine(LoadSceneRoutine(currentShopSceneName));
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

        pendingRewardType = RewardType.None;

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

        int nextStageNumber = currentStageNumber + 1;

        if (!HasStageSceneData(nextStageNumber))
        {
            Debug.Log("다음 스테이지 데이터가 없음 - 마지막 스테이지 클리어 처리 필요");
            isLoadingNextScene = false;
            return;
        }

        currentStageNumber = nextStageNumber;
        currentMapNumber = 1;
        pendingRewardType = RewardType.None;
        currentRoomType = RoomType.Combat;

        string firstSceneName = GetCombatSceneNameByMapNumber(currentMapNumber);

        if (string.IsNullOrEmpty(firstSceneName))
        {
            Debug.LogError("다음 스테이지 첫 씬 이름이 없음");
            isLoadingNextScene = false;
            return;
        }

        isLoadingNextScene = true;

        StartCoroutine(LoadSceneRoutine(firstSceneName));
    }

    private StageSceneData GetStageSceneData(int stageNumber)
    {
        if (stageSceneDatas == null)
            return null;

        for (int i = 0; i < stageSceneDatas.Length; i++)
        {
            StageSceneData data = stageSceneDatas[i];
            if (data == null) continue;

            if (data.stageNumber == stageNumber)
                return data;
        }

        // stageNumber를 비워뒀거나 잘못 넣었을 때를 위한 보조 처리
        int index = stageNumber - 1;

        if (index >= 0 && index < stageSceneDatas.Length)
            return stageSceneDatas[index];

        return null;
    }

    private bool HasStageSceneData(int stageNumber)
    {
        StageSceneData data = GetStageSceneData(stageNumber);

        if (data == null)
            return false;

        if (data.combatSceneNames == null || data.combatSceneNames.Length == 0)
            return false;

        return true;
    }

    private string GetCombatSceneNameByMapNumber(int mapNumber)
    {
        StageSceneData data = GetStageSceneData(currentStageNumber);

        if (data == null)
        {
            Debug.LogError($"스테이지 데이터가 없음 / currentStageNumber : {currentStageNumber}");
            return null;
        }

        if (data.combatSceneNames == null || data.combatSceneNames.Length == 0)
        {
            Debug.LogError($"전투 씬 배열이 비어있음 / currentStageNumber : {currentStageNumber}");
            return null;
        }

        int index = mapNumber - 1;

        if (index < 0 || index >= data.combatSceneNames.Length)
        {
            Debug.LogError($"전투 씬 인덱스 초과 / Stage : {currentStageNumber} / Map : {mapNumber}");
            return null;
        }

        return data.combatSceneNames[index];
    }

    private string GetCurrentShopSceneName()
    {
        StageSceneData data = GetStageSceneData(currentStageNumber);

        if (data == null)
            return null;

        return data.shopSceneName;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Time.timeScale = 1f;

        yield return new WaitForSeconds(clearDelay);

        SceneManager.LoadScene(sceneName);
    }

    private void GivePendingReward()
    {
        if (IsBossRoom())
        {
            pendingRewardType = RewardType.None;
            Debug.Log("보스룸이므로 이전 선택 보상 생성 차단");
            return;
        }

        if (pendingRewardType == RewardType.None)
            return;

        Debug.Log($"예약 보상 오브젝트 생성 : {pendingRewardType}");

        SpawnRewardObjectAtPlayer(pendingRewardType);

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
    
    public void OpenRewardUI(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.Augment:
                if (AugUIManager.instance != null)
                {
                    AugUIManager.instance.ShowAugmentation();
                }
                else
                {
                    Debug.LogWarning("AugUIManager.instance가 없어서 증강 UI를 열 수 없음");
                }

                break;

            case RewardType.Skill:
                if (SkillSelectUIManager.Instance != null)
                {
                    SkillSelectUIManager.Instance.IsSkillOpened = true;
                }
                else
                {
                    Debug.LogWarning("SkillSelectUIManager.Instance가 없어서 스킬 UI를 열 수 없음");
                }

                break;

            case RewardType.Item:
                if (NewItemUIManager.Instance != null)
                {
                    NewItemUIManager.Instance.IsOpenedItem = true;
                }
                else
                {
                    Debug.LogWarning("NewItemUIManager.Instance가 없어서 아이템 UI를 열 수 없음");
                }

                break;
        }

        currentRewardObject = null;
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

        StartCoroutine(OpenShopRoutesAfterSceneReady());
    }

    private IEnumerator OpenShopRoutesAfterSceneReady()
    {
        // 씬 로드 직후 모든 ExitSlot, markerSpawnPoint, 프리팹 위치가
        // 완전히 준비되도록 1프레임 기다린다.
        yield return null;

        CompleteShop();
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

    private bool IsMainScene()
    {
        return SceneManager.GetActiveScene().name == mainSceneName;
    }


    public void StartNewRun()
    {
        ResetRunStateOnly();

        string firstSceneName = GetCombatSceneNameByMapNumber(1);

        if (string.IsNullOrEmpty(firstSceneName))
        {
            Debug.LogError("첫 번째 전투 씬 이름이 없음");
            return;
        }

        SceneManager.LoadScene(firstSceneName);
    }

    public void ResetRunStateOnly()
    {
        currentStageNumber = 1;
        currentMapNumber = 1;
        currentRoomType = RoomType.Combat;
        pendingRewardType = RewardType.None;

        isStageCleared = false;
        isLoadingNextScene = false;

        currentOpenedRoutes.Clear();

        if (routeSlots != null)
        {
            LockAllRouteSlots();
        }

        routeSlots = null;

        Time.timeScale = 1f;

        Debug.Log("StageClear 런 상태 초기화 완료");
    }
    
    private Transform FindPlayerTransform()
    {
        if (PlayerController.Instance != null)
            return PlayerController.Instance.transform;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
            return playerObject.transform;

        return null;
    }
    
    private void SpawnRewardObjectAtPlayer(RewardType rewardType)
    {
        if (rewardType == RewardType.None)
            return;

        if (rewardInteractPrefab == null)
        {
            Debug.LogError("rewardInteractPrefab이 비어있음. StageClear 인스펙터에 보상 프리팹을 넣어야 함");
            return;
        }

        if (currentRewardObject != null)
        {
            Destroy(currentRewardObject);
            currentRewardObject = null;
        }

        Transform playerTransform = FindPlayerTransform();

        if (playerTransform == null)
        {
            Debug.LogError("플레이어를 찾지 못해서 보상 오브젝트를 생성할 수 없음");
            return;
        }

        Vector3 spawnPosition = playerTransform.position + new Vector3(0f, rewardSpawnYOffset, 0f);

        currentRewardObject = Instantiate(
            rewardInteractPrefab,
            spawnPosition,
            Quaternion.identity
        );

        RewardInteractObject rewardObject = currentRewardObject.GetComponent<RewardInteractObject>();

        if (rewardObject == null)
        {
            Debug.LogError("보상 프리팹에 RewardInteractObject 스크립트가 없음");
            return;
        }

        //rewardObject.Setup(rewardType);
    }
    
    private RewardIconData GetRewardIconData(RewardType rewardType)
    {
        if (rewardIconDatas == null)
            return null;

        for (int i = 0; i < rewardIconDatas.Length; i++)
        {
            if (rewardIconDatas[i] == null) continue;

            if (rewardIconDatas[i].rewardType == rewardType)
                return rewardIconDatas[i];
        }

        return null;
    }

    private Sprite GetRewardIcon(RewardType rewardType)
    {
        RewardIconData data = GetRewardIconData(rewardType);

        if (data != null)
            return data.icon;

        return null;
    }


    public int GetCurrentStageNumber()
    {
        return currentStageNumber;
    }

    public bool IsBossRoomPublic()
    {
        return currentRoomType == RoomType.Boss || currentMapNumber == 7;
    }

    public int GetCurrentStageNumberPublic()
    {
        return currentStageNumber;
    }

}