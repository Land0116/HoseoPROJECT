using UnityEngine;

public class UIUpDownFloat : MonoBehaviour
{
    [SerializeField] private float moveRange = 10f;
    [SerializeField] private float speed = 2f;

    private RectTransform rect;
    private Vector2 startPos;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rect != null)
            startPos = rect.anchoredPosition;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        float offsetY = Mathf.Sin(Time.unscaledTime * speed) * moveRange;

        rect.anchoredPosition = startPos + new Vector2(0f, offsetY);
    }

    private void OnDisable()
    {
        if (rect != null)
            rect.anchoredPosition = startPos;
    }
}