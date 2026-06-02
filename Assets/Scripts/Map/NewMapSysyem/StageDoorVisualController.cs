using System.Collections;
using UnityEngine;

public class StageDoorVisualController : MonoBehaviour
{
    public enum DoorOpenAnimationType
    {
        LeftOpen,
        RightOpen
    }

    [Header("닫힌 문 스프라이트 - Door 부모")]
    [SerializeField] private SpriteRenderer closedDoorRenderer;

    [Header("열리는 문 조각")]
    [SerializeField] private GameObject rightDoorObject;
    [SerializeField] private GameObject leftDoorObject;

    [Header("애니메이터")]
    [SerializeField] private Animator animator;

    [Header("문 열림 방향")]
    [SerializeField] private DoorOpenAnimationType openAnimationType = DoorOpenAnimationType.LeftOpen;

    [Header("Door 부모에 있는 문 막는 콜라이더")]
    [SerializeField] private Collider2D doorBlockCollider;

    [Header("열림 애니메이션 종료 후 문 조각 숨김")]
    [SerializeField] private bool hideDoorPiecesAfterOpenAnimation = true;

    [Header("애니메이션 이벤트 누락 대비용 시간")]
    [SerializeField] private float openAnimationFallbackTime = 0.6f;

    private static readonly int LeftOpenHash = Animator.StringToHash("LeftOpen");
    private static readonly int RightOpenHash = Animator.StringToHash("RightOpen");

    private bool isOpen;
    private bool openAnimationFinished;
    private Coroutine hideDoorPiecesRoutine;

    private void Awake()
    {
        AutoBind();
        SetClosedImmediate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBind();
    }

    private void Reset()
    {
        AutoBind();
    }
#endif

    private void AutoBind()
    {
        if (closedDoorRenderer == null)
            closedDoorRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (doorBlockCollider == null)
            doorBlockCollider = GetComponent<Collider2D>();

        if (rightDoorObject == null)
        {
            Transform right = FindChildRecursive(transform, "RightDoor");
            if (right != null)
                rightDoorObject = right.gameObject;
        }

        if (leftDoorObject == null)
        {
            Transform left = FindChildRecursive(transform, "LeftDoor");
            if (left != null)
                leftDoorObject = left.gameObject;
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == targetName)
                return child;

            Transform found = FindChildRecursive(child, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    public void SetClosedImmediate()
    {
        isOpen = false;
        openAnimationFinished = false;

        if (hideDoorPiecesRoutine != null)
        {
            StopCoroutine(hideDoorPiecesRoutine);
            hideDoorPiecesRoutine = null;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.ResetTrigger(LeftOpenHash);
            animator.ResetTrigger(RightOpenHash);
        }

        if (closedDoorRenderer != null)
            closedDoorRenderer.enabled = true;

        if (rightDoorObject != null)
            rightDoorObject.SetActive(false);

        if (leftDoorObject != null)
            leftDoorObject.SetActive(false);

        SetDoorBlockColliderEnabled(true);

        Debug.Log($"[StageDoor] Closed: {name}");
    }

    public void PlayOpen()
    {
        if (isOpen)
            return;

        isOpen = true;
        openAnimationFinished = false;

        Debug.Log($"[StageDoor] PlayOpen 호출됨: {name}");

        if (rightDoorObject != null)
            rightDoorObject.SetActive(true);

        if (leftDoorObject != null)
            leftDoorObject.SetActive(true);

        if (closedDoorRenderer != null)
            closedDoorRenderer.enabled = false;

        // 문 열림 → Door 부모의 큰 BoxCollider2D 끄기
        SetDoorBlockColliderEnabled(false);

        if (animator != null)
        {
            animator.ResetTrigger(LeftOpenHash);
            animator.ResetTrigger(RightOpenHash);

            switch (openAnimationType)
            {
                case DoorOpenAnimationType.LeftOpen:
                    animator.SetTrigger(LeftOpenHash);
                    break;

                case DoorOpenAnimationType.RightOpen:
                    animator.SetTrigger(RightOpenHash);
                    break;
            }
        }

        // 애니메이션 이벤트를 안 넣었을 때도 자동으로 꺼지게 하는 보험
        if (hideDoorPiecesAfterOpenAnimation)
        {
            if (hideDoorPiecesRoutine != null)
                StopCoroutine(hideDoorPiecesRoutine);

            hideDoorPiecesRoutine = StartCoroutine(HideDoorPiecesAfterDelay());
        }
    }

    private IEnumerator HideDoorPiecesAfterDelay()
    {
        yield return new WaitForSeconds(openAnimationFallbackTime);

        HideDoorPiecesAfterOpenAnimation();
    }

    /// <summary>
    /// LeftOpen / RightOpen 애니메이션 마지막 프레임에 Animation Event로 호출해도 됨.
    /// </summary>
    public void AnimationEvent_OnOpenAnimationEnd()
    {
        HideDoorPiecesAfterOpenAnimation();
    }

    private void HideDoorPiecesAfterOpenAnimation()
    {
        if (!isOpen)
            return;

        if (openAnimationFinished)
            return;

        openAnimationFinished = true;

        if (!hideDoorPiecesAfterOpenAnimation)
            return;

        if (rightDoorObject != null)
            rightDoorObject.SetActive(false);

        if (leftDoorObject != null)
            leftDoorObject.SetActive(false);

        Debug.Log($"[StageDoor] 열림 애니메이션 종료 → RightDoor / LeftDoor 비활성화: {name}");
    }

    private void SetDoorBlockColliderEnabled(bool enabled)
    {
        if (doorBlockCollider == null)
        {
            Debug.LogWarning($"[StageDoor] Door Block Collider가 없음: {name}");
            return;
        }

        // IsTrigger는 절대 건드리지 않는다.
        doorBlockCollider.enabled = enabled;

        Debug.Log(
            $"[StageDoor] Door Block Collider {(enabled ? "ON" : "OFF")}: {doorBlockCollider.name}, " +
            $"IsTrigger 유지값: {doorBlockCollider.isTrigger}"
        );
    }
}