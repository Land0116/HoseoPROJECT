using UnityEngine;

/// <summary>
/// 맵 출입구 하나를 관리하는 스크립트.
/// 
/// 역할:
/// - 이 출입구가 어느 방향 그룹인지 관리
/// - 다음 방 종류 관리
/// - 다음 전투방 보상 타입 관리
/// - 문 열림/닫힘 처리
/// - 출구 위 마커 생성
/// - 플레이어가 들어왔을 때 MapFlowManager에 다음 방 이동 요청
/// </summary>
public class GateController : AutoBindableBehaviour
{
    [Header("이 출입구의 그룹")] [SerializeField] private GateGroup gateGroup;

    [Header("이 문으로 이동할 방 종류")] [SerializeField]
    private RoomKind nextRoomKind = RoomKind.Combat;

    [Header("전투방으로 갈 경우 다음 전투방 보상")] [SerializeField]
    private RewardType rewardType = RewardType.None;

    [Header("문 막는 오브젝트")] [SerializeField] private GameObject blockObject;

    [Header("마커 생성 위치")] [SerializeField] private Transform markerSpawnPoint;

    [Header("플레이어 스폰 위치")] [SerializeField]
    private Transform playerSpawnPoint;

    [Header("공통 마커 프리팹")] [SerializeField] private RouteMarkerSpriteView routeMarkerPrefab;

    [Header("마커 아이콘")] [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite augmentIcon;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private Sprite bossIcon;
    [SerializeField] private Sprite combatIcon;

    [Header("스테이지 문 비주얼")]
    [SerializeField] private StageDoorVisualController stageDoorVisual;

    private RouteMarkerSpriteView currentMarkerView;
    private Collider2D gateCollider;
    private bool isOpen;

    public GateGroup GateGroup => gateGroup;
    public RoomKind NextRoomKind => nextRoomKind;
    public RewardType RewardType => rewardType;

    /// <summary>
    /// AutoBindableBehaviour에서 호출되는 실제 자동 바인딩 함수.
    /// 
    /// 기존 private AutoBind()에 있던 내용을 여기로 옮긴 것.
    /// </summary>
    protected override void AutoBindCore()
    {
        if (gateCollider == null)
        {
            gateCollider = GetComponent<Collider2D>();
        }

        if (gateCollider != null)
        {
            gateCollider.isTrigger = true;
        }

        AutoGuessGateGroupByName();

        // 기존 참조가 비어있거나, 자기 Gate의 자식이 아니면 다시 찾는다.
        if (blockObject == null || blockObject.transform.parent == null || !blockObject.transform.IsChildOf(transform))
        {
            Transform block = AutoBindUtility.FindChildRecursive(transform, "BlockObject");

            if (block != null)
            {
                blockObject = block.gameObject;
            }
        }

        if (markerSpawnPoint == null || !markerSpawnPoint.IsChildOf(transform))
        {
            markerSpawnPoint = AutoBindUtility.FindChildStartsWith(transform, "markerSpawnPoint");
        }

        if (playerSpawnPoint == null || !playerSpawnPoint.IsChildOf(transform))
        {
            playerSpawnPoint = AutoBindUtility.FindChildStartsWith(transform, "playerSpawnPoint");
        }
        if (stageDoorVisual == null)
        {
            stageDoorVisual = GetComponentInChildren<StageDoorVisualController>(true);
        }
    }

    /// <summary>
    /// 오브젝트 이름 앞글자를 기준으로 GateGroup을 자동 추정한다.
    /// 
    /// 예:
    /// A_GateLeft  -> GateGroup.A
    /// B_GateRight -> GateGroup.B
    /// C_GateUp    -> GateGroup.C
    /// D_GateDown  -> GateGroup.D
    /// 
    /// 하이라키 이름 규칙이 중요하다.
    /// </summary>
    private void AutoGuessGateGroupByName()
    {
        string objectName = gameObject.name;

        if (objectName.StartsWith("A"))
        {
            gateGroup = GateGroup.A;
        }
        else if (objectName.StartsWith("B"))
        {
            gateGroup = GateGroup.B;
        }
        else if (objectName.StartsWith("C"))
        {
            gateGroup = GateGroup.C;
        }
        else if (objectName.StartsWith("D"))
        {
            gateGroup = GateGroup.D;
        }
    }

    /// <summary>
    /// 플레이어가 이 문으로 들어왔을 때 배치될 위치를 반환한다.
    /// 
    /// playerSpawnPoint가 있으면 그 위치 사용.
    /// 없으면 문 오브젝트 자신의 위치 사용.
    /// </summary>
    public Vector3 GetPlayerSpawnPosition()
    {
        if (playerSpawnPoint != null)
        {
            /*Debug.Log(
                $"[Gate SpawnPoint] Gate={name}, GateGroup={gateGroup}, " +
                $"SpawnPoint={playerSpawnPoint.name}, " +
                $"SpawnPointParent={playerSpawnPoint.parent.name}, " +
                $"Position={playerSpawnPoint.position}"
            );*/

            return playerSpawnPoint.position;
        }

        //Debug.LogWarning($"[Gate SpawnPoint] {name}에 playerSpawnPoint가 없음. Gate 위치 사용.");
        return transform.position;
    }

    /// <summary>
    /// 문을 열거나 닫는다.
    /// 
    /// open == true:
    /// - 출입구 콜라이더 활성화
    /// - 막는 오브젝트 비활성화
    /// - 마커 생성
    /// 
    /// open == false:
    /// - 출입구 콜라이더 비활성화
    /// - 막는 오브젝트 활성화
    /// - 마커 제거
    /// </summary>
    public void SetOpen(bool open)
    {
        isOpen = open;

        if (gateCollider != null)
        {
            gateCollider.isTrigger = true;
            gateCollider.enabled = open;
        }

        if (blockObject != null)
        {
            blockObject.SetActive(!open);
        }

        if (stageDoorVisual != null)
        {
            if (open)
                stageDoorVisual.PlayOpen();
            else
                stageDoorVisual.SetClosedImmediate();
        }

        if (open)
        {
            SpawnMarker();
        }
        else
        {
            DestroyMarker();
        }
    }

    /// <summary>
    /// 이 문으로 들어갔을 때 이동할 다음 방 정보를 설정한다.
    /// 
    /// 예:
    /// SetRoute(RoomKind.Shop, RewardType.None)
    /// SetRoute(RoomKind.Combat, RewardType.Augment)
    /// SetRoute(RoomKind.Boss, RewardType.None)
    /// </summary>
    public void SetRoute(RoomKind roomKind, RewardType newRewardType)
    {
        nextRoomKind = roomKind;
        rewardType = newRewardType;

        if (isOpen)
        {
            SpawnMarker();
        }
    }

    /// <summary>
    /// 현재 route 정보에 맞는 마커를 생성한다.
    /// 
    /// 이미 마커가 있으면 먼저 제거 후 다시 생성한다.
    /// 이렇게 해야 SetRoute()가 바뀌었을 때 아이콘이 갱신된다.
    /// </summary>
    private void SpawnMarker()
    {
        DestroyMarker();

        if (routeMarkerPrefab == null)
        {
            //Debug.LogWarning($"[{name}] RouteMarkerPrefab이 없음");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        if (markerSpawnPoint != null)
        {
            spawnPosition = markerSpawnPoint.position;
            spawnRotation = markerSpawnPoint.rotation;
        }

        currentMarkerView = Instantiate(routeMarkerPrefab, spawnPosition, spawnRotation);

        currentMarkerView.transform.SetParent(transform, true);

        Sprite icon = GetMarkerIcon();

        currentMarkerView.Setup(icon);
    }

    /// <summary>
    /// 현재 생성된 마커를 제거한다.
    /// </summary>
    private void DestroyMarker()
    {
        if (currentMarkerView == null) return;

        Destroy(currentMarkerView.gameObject);
        currentMarkerView = null;
    }

    /// <summary>
    /// 현재 nextRoomKind와 rewardType에 맞는 마커 아이콘을 반환한다.
    /// </summary>
    private Sprite GetMarkerIcon()
    {
        if (nextRoomKind == RoomKind.Shop)
        {
            return shopIcon;
        }

        if (nextRoomKind == RoomKind.Boss)
        {
            return bossIcon;
        }

        if (nextRoomKind == RoomKind.Combat)
        {
            switch (rewardType)
            {
                case RewardType.Augment:
                    return augmentIcon;

                case RewardType.Skill:
                    return skillIcon;

                case RewardType.Item:
                    return itemIcon;

                case RewardType.None:
                    return combatIcon;
            }
        }

        return null;
    }

    /// <summary>
    /// 플레이어가 열린 문에 들어왔을 때 다음 방으로 이동한다.
    /// 
    /// 보스방에서 나가는 경우:
    /// - 보스 클리어 처리
    /// - 다음 액트로 이동
    /// 
    /// 일반 방에서 나가는 경우:
    /// - 선택된 문 정보 기준으로 다음 방 이동
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isOpen) return;
        if (!collision.CompareTag("Player")) return;
        if (MapFlowManager.Instance == null) return;

        if (MapFlowManager.Instance.CurrentRoomKind == RoomKind.Boss)
        {
            BossRoomController bossRoom = FindAnyObjectByType<BossRoomController>();

            if (bossRoom != null)
            {
                bossRoom.TryEnterBossClearGate(gateGroup);
            }
            else
            {
                Debug.LogWarning("[GateController] BossRoomController를 찾지 못함");
            }

            return;
        }

        MapFlowManager.Instance.EnterNextRoomFromGate(gateGroup, nextRoomKind, rewardType);
    }

    /// <summary>
    /// 문 오브젝트가 파괴될 때 생성된 마커도 같이 제거한다.
    /// 씬 전환 시 남는 오브젝트 방지용.
    /// </summary>
    private void OnDestroy()
    {
        DestroyMarker();
    }
}