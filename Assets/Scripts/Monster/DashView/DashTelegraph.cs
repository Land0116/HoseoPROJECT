using UnityEngine;

public class DashTelegraph : MonoBehaviour
{
    [SerializeField] private Transform baseTransform;
    [SerializeField] private Transform fillTransform;

    private bool isActive = true; //

    private float maxLength;
    private float duration;
    private float currentTime;

    private Vector2 direction;

    private Vector3 startPos;
    public void Init(Vector2 dir, float length, float time)
    {
        isActive = true;//
        startPos = transform.position;

        direction = dir.normalized;
        maxLength = length;
        duration = time;
        currentTime = 0f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        transform.position = startPos;

        baseTransform.localPosition = new Vector3(maxLength * 0.5f, 0f, 0f);
        baseTransform.localScale = new Vector3(maxLength, baseTransform.localScale.y, 1f);

        fillTransform.localPosition = new Vector3(0f, 0f, 0f);
        fillTransform.localScale = new Vector3(0f, fillTransform.localScale.y, 1f);
    }

    void Update()
    {
        if (!isActive) return;//
        currentTime += Time.deltaTime;

        float t = Mathf.Clamp01(currentTime / duration);
        float currentLength = Mathf.Lerp(0f, maxLength, t);

        fillTransform.localScale = new Vector3(currentLength, fillTransform.localScale.y, 1f);

        fillTransform.localPosition = new Vector3(currentLength * 0.5f, 0f, 0f);
    }
    private void OnDestroy()
    {
        isActive = false;
    }
}