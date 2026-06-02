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

    private static readonly int LeftOpenHash = Animator.StringToHash("LeftOpen");
    private static readonly int RightOpenHash = Animator.StringToHash("RightOpen");

    private bool isOpen;

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

        Debug.Log($"[StageDoor] PlayOpen 호출됨: {name}");

        if (rightDoorObject != null)
            rightDoorObject.SetActive(true);

        if (leftDoorObject != null)
            leftDoorObject.SetActive(true);

        if (closedDoorRenderer != null)
            closedDoorRenderer.enabled = false;

        // 핵심: 문 열림 → Door 부모의 큰 BoxCollider2D 끄기
        SetDoorBlockColliderEnabled(false);

        if (animator == null)
            return;

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

    private void SetDoorBlockColliderEnabled(bool enabled)
    {
        if (doorBlockCollider == null)
        {
            Debug.LogWarning($"[StageDoor] Door Block Collider가 없음: {name}");
            return;
        }

        // Door의 콜라이더는 문 막는 용도라서 Trigger가 아니어야 함
        doorBlockCollider.enabled = enabled;

        Debug.Log($"[StageDoor] Door Block Collider {(enabled ? "ON" : "OFF")}: {doorBlockCollider.name}");
    }
}