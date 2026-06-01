using UnityEngine;

public class StageDoorVisualController : MonoBehaviour
{
    public enum DoorOpenAnimationType
    {
        LeftOpen,
        RightOpen
    }

    [Header("닫힌 문 스프라이트 - 부모 오브젝트")]
    [SerializeField] private SpriteRenderer closedDoorRenderer;

    [Header("열리는 문 조각")]
    [SerializeField] private GameObject leftDoorObject;
    [SerializeField] private GameObject rightDoorObject;

    [Header("애니메이터")]
    [SerializeField] private Animator animator;

    [Header("문 열림 방향")]
    [SerializeField] private DoorOpenAnimationType openAnimationType = DoorOpenAnimationType.LeftOpen;

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
        {
            closedDoorRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (leftDoorObject == null)
        {
            Transform left = FindChildRecursive(transform, "LeftDoor");
            if (left != null)
                leftDoorObject = left.gameObject;
        }

        if (rightDoorObject == null)
        {
            Transform right = FindChildRecursive(transform, "RightDoor");
            if (right != null)
                rightDoorObject = right.gameObject;
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;

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

        if (leftDoorObject != null)
            leftDoorObject.SetActive(false);

        if (rightDoorObject != null)
            rightDoorObject.SetActive(false);
    }

    public void PlayOpen()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (leftDoorObject != null)
            leftDoorObject.SetActive(true);

        if (rightDoorObject != null)
            rightDoorObject.SetActive(true);

        if (closedDoorRenderer != null)
            closedDoorRenderer.enabled = false;

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

    public void AnimationEvent_HideClosedDoor()
    {
        if (closedDoorRenderer != null)
            closedDoorRenderer.enabled = false;

        if (leftDoorObject != null)
            leftDoorObject.SetActive(true);

        if (rightDoorObject != null)
            rightDoorObject.SetActive(true);
    }
}