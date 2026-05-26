using System.Collections;
using UnityEngine;

public class FieldRewardDropMotion : MonoBehaviour
{
    [Header("움직일 시각 오브젝트")]
    [SerializeField] private Transform visualRoot;

    [Header("그림자")]
    [SerializeField] private Transform shadowRoot;

    [Header("튀어오르는 높이")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("옆으로 떨어지는 거리")]
    [SerializeField] private float horizontalDistance = 0.4f;

    [Header("연출 시간")]
    [SerializeField] private float duration = 0.45f;

    [Header("착지 후 튕김")]
    [SerializeField] private float bounceHeight = 0.18f;

    [SerializeField] private float bounceDuration = 0.12f;

    [Header("그림자 크기 변화")]
    [SerializeField] private float minShadowScaleMultiplier = 0.75f;

    private Coroutine dropRoutine;

    private Vector3 visualDefaultLocalPos;
    private Vector3 shadowDefaultLocalScale;

    private bool hasCachedDefault;

    private void Awake()
    {
        AutoBind();
        CacheDefaultTransform();
    }

    private void Reset()
    {
        AutoBind();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBind();
    }
#endif

    private void AutoBind()
    {
        if (visualRoot == null)
        {
            Transform found = transform.Find("VisualRoot");

            if (found != null)
            {
                visualRoot = found;
            }
        }

        if (shadowRoot == null)
        {
            Transform found = transform.Find("Shadow");

            if (found != null)
            {
                shadowRoot = found;
            }
        }
    }

    private void CacheDefaultTransform()
    {
        if (visualRoot != null)
        {
            visualDefaultLocalPos = visualRoot.localPosition;
        }

        if (shadowRoot != null)
        {
            shadowDefaultLocalScale = shadowRoot.localScale;
        }

        hasCachedDefault = true;
    }

    public void Play()
    {
        if (!hasCachedDefault)
        {
            CacheDefaultTransform();
        }

        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
        }

        dropRoutine = StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        if (visualRoot == null)
        {
            yield break;
        }

        Vector3 startLocalPos = visualDefaultLocalPos + new Vector3(
            Random.Range(-horizontalDistance, horizontalDistance),
            0f,
            0f
        );

        Vector3 endLocalPos = visualDefaultLocalPos;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);

            float x = Mathf.Lerp(startLocalPos.x, endLocalPos.x, t);

            float height01 = Mathf.Sin(t * Mathf.PI);
            float y = endLocalPos.y + height01 * jumpHeight;

            visualRoot.localPosition = new Vector3(x, y, endLocalPos.z);

            UpdateShadow(height01);

            yield return null;
        }

        time = 0f;

        while (time < bounceDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / bounceDuration);

            float height01 = Mathf.Sin(t * Mathf.PI);
            float y = visualDefaultLocalPos.y + height01 * bounceHeight;

            visualRoot.localPosition = new Vector3(
                visualDefaultLocalPos.x,
                y,
                visualDefaultLocalPos.z
            );

            UpdateShadow(height01);

            yield return null;
        }

        visualRoot.localPosition = visualDefaultLocalPos;

        if (shadowRoot != null)
        {
            shadowRoot.localScale = shadowDefaultLocalScale;
        }

        dropRoutine = null;
    }

    private void UpdateShadow(float height01)
    {
        if (shadowRoot == null)
        {
            return;
        }

        float scaleMultiplier = Mathf.Lerp(1f, minShadowScaleMultiplier, height01);

        shadowRoot.localScale = new Vector3(
            shadowDefaultLocalScale.x * scaleMultiplier,
            shadowDefaultLocalScale.y * scaleMultiplier,
            shadowDefaultLocalScale.z
        );
    }
}