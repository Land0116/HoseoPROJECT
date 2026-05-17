using UnityEngine;

public class TriangleTelegraph : MonoBehaviour
{
    [Header("Triangle Objects")]
    [SerializeField] private Transform baseTransform;
    [SerializeField] private Transform fillTransform;

    private float duration;
    private float currentTime;

    private Vector2 direction;

    private bool isFilling = false;

    private Vector3 originalScale;

    public void Init(Vector2 dir, float length, float width, float time)
    {
        direction = dir.normalized;
        duration = time;
        currentTime = 0f;

        // 방향 회전 (+90도 보정 유지)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        originalScale = fillTransform.localScale;

        fillTransform.localScale = new Vector3(
            0f,
            originalScale.y,
            originalScale.z
        );
    }

    public void StartFill(float time)
    {
        duration = time;
        currentTime = 0f;
        isFilling = true;
    }

    void Update()
    {
        if (!isFilling) return;

        currentTime += Time.deltaTime;

        float t = Mathf.Clamp01(currentTime / duration);

        fillTransform.localScale = new Vector3(
            originalScale.x * t,
            originalScale.y,
            originalScale.z
        );
    }
}