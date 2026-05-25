using UnityEngine;

public class ShadowVisualController : MonoBehaviour
{
    [Header("그림자")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("기본값")]
    [SerializeField] private Vector3 baseLocalPosition = new Vector3(0f, -0.85f, 0f);
    [SerializeField] private Vector3 baseLocalScale = new Vector3(1.15f, 0.55f, 1f);
    [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.35f;

    [Header("일반 이동 - 위치 변화")]
    [SerializeField] private float horizontalOffsetAmount = 0.12f;
    [SerializeField] private float verticalOffsetAmount = 0.10f;

    [Header("일반 이동 - 앞쪽 이동")]
    [SerializeField] private float forwardScaleXBonus = 0.35f;
    [SerializeField] private float forwardScaleYBonus = -0.13f;
    [SerializeField] private float forwardAlphaBonus = 0.08f;

    [Header("일반 이동 - 뒤쪽 이동")]
    [SerializeField] private float backwardScaleXBonus = -0.12f;
    [SerializeField] private float backwardScaleYBonus = -0.04f;
    [SerializeField] private float backwardAlphaBonus = -0.06f;

    [Header("일반 이동 - 좌우 이동")]
    [SerializeField] private float sideScaleXBonus = 0.22f;
    [SerializeField] private float sideScaleYBonus = -0.08f;
    [SerializeField] private float sideAlphaBonus = 0.04f;

    [Header("대쉬 그림자 효과")]
    [SerializeField] private float dashBackOffsetAmount = 0.22f;
    [SerializeField] private float dashScaleXBonus = 0.45f;
    [SerializeField] private float dashScaleYBonus = -0.22f;
    [SerializeField] private float dashAlphaBonus = 0.18f;

    [Header("보간 속도")]
    [SerializeField] private float normalLerpSpeed = 12f;
    [SerializeField] private float dashLerpSpeed = 22f;

    private Vector3 targetLocalPosition;
    private Vector3 targetLocalScale;
    private float targetAlpha;
    private float currentLerpSpeed;

    private void Awake()
    {
        AutoBind();
        ApplyImmediateDefault();
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
        if (shadowTransform == null)
        {
            shadowTransform = transform;
        }

        if (shadowRenderer == null)
        {
            shadowRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void ApplyImmediateDefault()
    {
        targetLocalPosition = baseLocalPosition;
        targetLocalScale = baseLocalScale;
        targetAlpha = baseAlpha;
        currentLerpSpeed = normalLerpSpeed;

        if (shadowTransform != null)
        {
            shadowTransform.localPosition = targetLocalPosition;
            shadowTransform.localScale = targetLocalScale;
        }

        SetAlpha(targetAlpha);
    }

    public void UpdateShadow(Vector2 moveDirection, bool isMoving, bool isDashing, Vector2 dashDirection)
    {
        Vector2 dir = moveDirection.sqrMagnitude > 0.0001f
            ? moveDirection.normalized
            : Vector2.zero;

        if (isDashing && dashDirection.sqrMagnitude > 0.0001f)
        {
            dir = dashDirection.normalized;
        }

        targetLocalPosition = baseLocalPosition;
        targetLocalScale = baseLocalScale;
        targetAlpha = baseAlpha;
        currentLerpSpeed = isDashing ? dashLerpSpeed : normalLerpSpeed;

        if (!isMoving && !isDashing)
        {
            return;
        }

        ApplyMoveDirectionEffect(dir);

        if (isDashing)
        {
            ApplyDashEffect(dir);
        }

        targetAlpha = Mathf.Clamp01(targetAlpha);
    }

    private void ApplyMoveDirectionEffect(Vector2 dir)
    {
        // 좌우 이동 시 그림자가 옆으로 따라가며 방향감을 줌
        targetLocalPosition.x += dir.x * horizontalOffsetAmount;

        // 아래로 이동하면 그림자가 더 아래쪽으로 내려가서 앞으로 오는 느낌
        targetLocalPosition.y -= dir.y * verticalOffsetAmount;

        // 아래쪽 이동: 화면 앞으로 오는 느낌
        if (dir.y < -0.1f)
        {
            float t = Mathf.Abs(dir.y);

            targetLocalScale.x += forwardScaleXBonus * t;
            targetLocalScale.y += forwardScaleYBonus * t;
            targetAlpha += forwardAlphaBonus * t;
        }
        // 위쪽 이동: 멀어지는 느낌
        else if (dir.y > 0.1f)
        {
            float t = Mathf.Abs(dir.y);

            targetLocalScale.x += backwardScaleXBonus * t;
            targetLocalScale.y += backwardScaleYBonus * t;
            targetAlpha += backwardAlphaBonus * t;
        }

        // 좌우 이동: 옆으로 흐르는 느낌
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            float t = Mathf.Abs(dir.x);

            targetLocalScale.x += sideScaleXBonus * t;
            targetLocalScale.y += sideScaleYBonus * t;
            targetAlpha += sideAlphaBonus * t;
        }
    }

    private void ApplyDashEffect(Vector2 dashDir)
    {
        /*
         * 대쉬 방향의 반대쪽으로 그림자를 민다.
         * 예:
         * 오른쪽 대쉬 → 그림자는 살짝 왼쪽으로 밀림
         * 아래쪽 대쉬 → 그림자는 살짝 위쪽으로 밀림
         */
        targetLocalPosition -= (Vector3)(dashDir.normalized * dashBackOffsetAmount);

        // 대쉬 중에는 바닥에 강하게 눌린 느낌
        targetLocalScale.x += dashScaleXBonus;
        targetLocalScale.y += dashScaleYBonus;

        // 빠르게 움직이는 순간 그림자가 진해지는 느낌
        targetAlpha += dashAlphaBonus;
    }

    private void LateUpdate()
    {
        if (shadowTransform != null)
        {
            shadowTransform.localPosition = Vector3.Lerp(
                shadowTransform.localPosition,
                targetLocalPosition,
                Time.deltaTime * currentLerpSpeed
            );

            shadowTransform.localScale = Vector3.Lerp(
                shadowTransform.localScale,
                targetLocalScale,
                Time.deltaTime * currentLerpSpeed
            );
        }

        if (shadowRenderer != null)
        {
            Color color = shadowRenderer.color;
            color.a = Mathf.Lerp(
                color.a,
                targetAlpha,
                Time.deltaTime * currentLerpSpeed
            );
            shadowRenderer.color = color;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (shadowRenderer == null) return;

        Color color = shadowRenderer.color;
        color.a = alpha;
        shadowRenderer.color = color;
    }

    public void SetVisible(bool visible)
    {
        if (shadowRenderer != null)
        {
            shadowRenderer.enabled = visible;
        }
    }
}