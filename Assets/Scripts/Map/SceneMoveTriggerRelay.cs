using TMPro;
using UnityEngine;

public class SceneMoveTriggerRelay : MonoBehaviour
{
    [Header("현재 이 출구에 배정된 경로 타입")]
    [SerializeField] private StageClear.RouteType routeType = StageClear.RouteType.None;
    
    [Header("보상 표시 생성 위치")]
    [SerializeField] private Transform markerSpawnPoint;
    private GameObject currentMarkerObject;

    [Header("SpawnPoint가 없을 때 사용할 로컬 위치")]
    [SerializeField] private Vector3 markerLocalOffset = new Vector3(0f, 1.5f, 0f);

    private Collider2D moveCollider;
    private GameObject currentMarker;

    private void Awake()
    {
        moveCollider = GetComponent<Collider2D>();
        
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

        if (!isEnabled)
        {
            ClearRouteMarker();
        }

    }

    private void RemoveRouteMarker()
    {
        if (currentMarker == null) return;

        Destroy(currentMarker);
        currentMarker = null;
    }
    
    public void ClearRouteMarker()
    {
        if (currentMarkerObject != null)
        {
            Destroy(currentMarkerObject);
            currentMarkerObject = null;
        }
    }

    public void ShowSpriteRouteMarker(GameObject markerPrefab, string rewardName, Sprite icon)
    {
        ClearRouteMarker();

        if (markerPrefab == null)
            return;
        
        Transform spawnPoint = markerSpawnPoint != null ? markerSpawnPoint : transform;
        
        currentMarkerObject = Instantiate(
            markerPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            spawnPoint
        );

        currentMarkerObject.transform.localPosition = Vector3.zero;
        currentMarkerObject.transform.localRotation = Quaternion.identity;
        currentMarkerObject.transform.localScale = Vector3.one;

        RouteMarkerSpriteView spriteView = currentMarkerObject.GetComponent<RouteMarkerSpriteView>();

        if (spriteView != null)
        {
            spriteView.Setup(icon);
            return;
        }
        
    }
}