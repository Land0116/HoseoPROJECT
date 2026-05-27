/*using UnityEngine;

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
}*/
using UnityEngine;
using UnityEngine.UI;

public class UIUpDownFloat : MonoBehaviour
{
    [SerializeField] private float moveRange = 10f;
    [SerializeField] private float speed = 2f;

    private RectTransform rect;
    private Vector2 startPos;
    private Image image;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        startPos = rect.anchoredPosition;


        if (image != null)
        {
            Material mat = new Material(Shader.Find("UI/Default"));
            image.material = mat;
        }


        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
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

        float finalY = startPos.y + offsetY;
        finalY = Mathf.Round(finalY * 2f) / 2f;

        rect.anchoredPosition = new Vector2(startPos.x, finalY);
    }

    private void OnDisable()
    {
        if (rect != null)
            rect.anchoredPosition = startPos;
    }
}