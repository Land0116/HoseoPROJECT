using UnityEngine;

public class LowerPlayerVisual : MonoBehaviour
{
    [Header("하체 컴포넌트")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("대각선 입력 보정")]
    [SerializeField] private float diagonalReleaseGraceTime = 0.12f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMoveHash = Animator.StringToHash("IsMove");

    private Vector2 lastMoveDirection = Vector2.down;
    private Vector2 lastDiagonalDirection = Vector2.down;

    private float lastDiagonalInputTime = -999f;
    private bool wasMoving;

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
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void SetMovement(Vector2 moveInput, float moveSpeedMultiplier)
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            Vector2 eightDirection = GetEightDirection(moveInput);

            if (IsDiagonalDirection(eightDirection))
            {
                lastMoveDirection = eightDirection;
                lastDiagonalDirection = eightDirection;
                lastDiagonalInputTime = Time.time;
            }
            else
            {
                /*
                 * 핵심:
                 * 대각선 입력 직후 아주 짧게 상/하/좌/우 값이 들어오는 경우가 있음.
                 * 이건 플레이어가 방향을 바꾼 게 아니라 키를 떼는 과정에서 생기는 입력 찌꺼기임.
                 * 그래서 grace 시간 안에는 마지막 대각선 방향을 유지한다.
                 */
                bool recentlyUsedDiagonal =
                    Time.time - lastDiagonalInputTime <= diagonalReleaseGraceTime;

                if (recentlyUsedDiagonal)
                {
                    lastMoveDirection = lastDiagonalDirection;
                }
                else
                {
                    lastMoveDirection = eightDirection;
                }
            }

            animator.speed = Mathf.Clamp(moveSpeedMultiplier, 0.1f, 3f);
        }
        else
        {
            /*
             * 멈췄을 때 MoveX/MoveY를 0으로 보내면 안 됨.
             * 마지막 방향을 그대로 유지해야 Idle Blend Tree가 같은 방향을 출력함.
             */
            animator.speed = 1f;
        }

        animator.SetFloat(MoveXHash, lastMoveDirection.x);
        animator.SetFloat(MoveYHash, lastMoveDirection.y);
        animator.SetBool(IsMoveHash, isMoving);

        wasMoving = isMoving;
    }

    private Vector2 GetEightDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
            return lastMoveDirection;

        Vector2 normalized = input.normalized;

        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        if (angle >= 337.5f || angle < 22.5f)
            return Vector2.right;

        if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(0.707f, 0.707f);

        if (angle >= 67.5f && angle < 112.5f)
            return Vector2.up;

        if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(-0.707f, 0.707f);

        if (angle >= 157.5f && angle < 202.5f)
            return Vector2.left;

        if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(-0.707f, -0.707f);

        if (angle >= 247.5f && angle < 292.5f)
            return Vector2.down;

        return new Vector2(0.707f, -0.707f);
    }

    private bool IsDiagonalDirection(Vector2 direction)
    {
        return Mathf.Abs(direction.x) > 0.1f &&
               Mathf.Abs(direction.y) > 0.1f;
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;

        if (!visible && animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(IsMoveHash, false);
        }
    }
}