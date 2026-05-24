using UnityEngine;

public class UpperPlayerVisual : MonoBehaviour
{
    [Header("상체 컴포넌트")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private static readonly int MoveXHash = Animator.StringToHash("AimX");
    private static readonly int MoveYHash = Animator.StringToHash("AimY");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsChargeHash = Animator.StringToHash("IsCharge");
    private static readonly int IsMoveHash = Animator.StringToHash("IsMove");

    private Vector2 lastAimDirection = Vector2.down;

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

    public void SetAimDirection(Vector2 direction)
    {
        if (animator == null) return;

        if (direction.sqrMagnitude > 0.0001f)
        {
            lastAimDirection = direction.normalized;
        }

        animator.SetFloat(MoveXHash, lastAimDirection.x);
        animator.SetFloat(MoveYHash, lastAimDirection.y);
    }

    public void PlayAttack(Vector2 attackDirection, float attackSpeedMultiplier)
    {
        if (animator == null) return;

        SetVisible(true);
        SetAimDirection(attackDirection);

        animator.speed = Mathf.Clamp(attackSpeedMultiplier, 0.1f, 10f);

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }

    public void SetCharge(bool value, Vector2 chargeDirection)
    {
        if (animator == null) return;

        SetVisible(true);
        SetAimDirection(chargeDirection);

        animator.SetBool(IsChargeHash, value);

        if (!value)
        {
            animator.speed = 1f;
        }
    }

    public void ResetAttackVisual()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.ResetTrigger(AttackHash);
        animator.SetBool(IsChargeHash, false);
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
            animator.ResetTrigger(AttackHash);
            animator.SetBool(IsChargeHash, false);
        }
    }
    
    public bool IsInAttackState()
    {
        if (animator == null)
            return false;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Attack"))
            return true;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);

            if (nextState.IsTag("Attack"))
                return true;
        }

        return false;
    }

    public void SetMoveState(bool isMove)
    {
        if (animator == null)
            return;

        animator.SetBool(IsMoveHash, isMove);
    }
}