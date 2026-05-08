using TMPro;
using UnityEngine;

public class SceneMoveTriggerRelay : MonoBehaviour
{
    [Header("현재 이 출구에 배정된 경로 타입")]
    [SerializeField] private StageClear.RouteType routeType = StageClear.RouteType.None;

    [Header("출구 보상 표시 프리팹")]
    [SerializeField] private GameObject routeMarkerPrefab;

    [Header("보상 표시 생성 위치")]
    [SerializeField] private Transform markerSpawnPoint;

    [Header("SpawnPoint가 없을 때 사용할 로컬 위치")]
    [SerializeField] private Vector3 markerLocalOffset = new Vector3(0f, 1.5f, 0f);

    private Collider2D moveCollider;
    private GameObject currentMarker;

    private void Awake()
    {
        moveCollider = GetComponent<Collider2D>();

        // 씬 시작 시 출구는 막힌 상태.
        // 오브젝트는 끄지 않고 Collider만 Trigger 해제.
        SetRouteType(StageClear.RouteType.None);
        SetRouteEnabled(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (StageClear.Instance != null)
        {
            StageClear.Instance.TryMoveNextScene(other, routeType);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (StageClear.Instance != null)
        {
            StageClear.Instance.TryMoveNextScene(other, routeType);
        }
    }

    public void SetRouteType(StageClear.RouteType newRouteType)
    {
        routeType = newRouteType;
    }

    public StageClear.RouteType GetRouteType()
    {
        return routeType;
    }

    public void SetRouteEnabled(bool isEnabled)
    {
        if (moveCollider == null)
            moveCollider = GetComponent<Collider2D>();

        if (moveCollider != null)
        {
            // false면 벽처럼 막힘.
            // true면 출구로 이동 가능.
            moveCollider.isTrigger = isEnabled;
        }

        if (isEnabled)
        {
            CreateRouteMarker();
        }
        else
        {
            RemoveRouteMarker();
        }
    }

    private void CreateRouteMarker()
    {
        if (routeType == StageClear.RouteType.None) return;
        if (routeMarkerPrefab == null) return;

        RemoveRouteMarker();

        if (markerSpawnPoint != null)
        {
            currentMarker = Instantiate(
                routeMarkerPrefab,
                markerSpawnPoint.position,
                markerSpawnPoint.rotation,
                markerSpawnPoint
            );

            currentMarker.transform.localPosition = Vector3.zero;
        }
        else
        {
            currentMarker = Instantiate(
                routeMarkerPrefab,
                transform
            );

            currentMarker.transform.localPosition = markerLocalOffset;
            currentMarker.transform.localRotation = Quaternion.identity;
        }

        TMP_Text markerText = currentMarker.GetComponentInChildren<TMP_Text>(true);

        if (markerText != null)
        {
            markerText.text = GetRouteDisplayName(routeType);
        }
    }

    private void RemoveRouteMarker()
    {
        if (currentMarker == null) return;

        Destroy(currentMarker);
        currentMarker = null;
    }

    private string GetRouteDisplayName(StageClear.RouteType type)
    {
        switch (type)
        {
            case StageClear.RouteType.Shop:
                return "상점";

            case StageClear.RouteType.Augment:
                return "증강";

            case StageClear.RouteType.Skill:
                return "스킬";

            case StageClear.RouteType.Item:
                return "아이템";

            case StageClear.RouteType.Boss:
                return "보스";

            case StageClear.RouteType.NextStage:
                return "다음 스테이지";

            default:
                return "";
        }
    }
}