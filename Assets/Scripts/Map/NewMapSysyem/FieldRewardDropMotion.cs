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

    private Coroutine dropRoutine;

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

    public void Play()
    {
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

        Vector3 startLocalPos = new Vector3(
            Random.Range(-horizontalDistance, horizontalDistance),
            0f,
            0f
        );

        Vector3 endLocalPos = Vector3.zero;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);

            float x = Mathf.Lerp(startLocalPos.x, endLocalPos.x, t);

            // 포물선 높이
            float y = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            visualRoot.localPosition = new Vector3(x, y, 0f);

            if (shadowRoot != null)
            {
                float shadowScale = Mathf.Lerp(1f, 1f, t);
                shadowRoot.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }

            yield return null;
        }

        time = 0f;

        while (time < bounceDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / bounceDuration);
            float y = Mathf.Sin(t * Mathf.PI) * bounceHeight;

            visualRoot.localPosition = new Vector3(0f, y, 0f);

            yield return null;
        }

        visualRoot.localPosition = Vector3.zero;

        if (shadowRoot != null)
        {
            shadowRoot.localScale = Vector3.one;
        }

        dropRoutine = null;
    }
}