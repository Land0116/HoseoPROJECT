using UnityEngine;

public class LowerPlayerVisual : MonoBehaviour
{
    [Header("하체 컴포넌트")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMoveHash = Animator.StringToHash("IsMove");

    private Vector2 lastMoveDirection = Vector2.down;

    private void Awake()
    {
        AutoBind();
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
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void SetMovement(Vector2 moveInput, float moveSpeedMultiplier)
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            lastMoveDirection = moveInput.normalized;

            animator.SetFloat(MoveXHash, lastMoveDirection.x);
            animator.SetFloat(MoveYHash, lastMoveDirection.y);
            animator.speed = Mathf.Clamp(moveSpeedMultiplier, 0.1f, 3f);
        }
        else
        {
            animator.speed = 1f;
        }

        animator.SetBool(IsMoveHash, isMoving);
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (!visible && animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(IsMoveHash, false);
        }
    }
}