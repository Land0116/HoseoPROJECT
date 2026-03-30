using System.Collections;
using UnityEngine;

public class BossUI : MonoBehaviour
{
    [SerializeField] private RectTransform panel; // BossUIPanel
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float stayTime = 2f;
    [SerializeField] private float offScreenMultiplier = 2f;

    private Vector3 leftOffScreen;
    private Vector3 center;
    private Vector3 rightOffScreen;

    private bool hasShown = false;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponentInChildren<RectTransform>();
    }

    private void Start()
    {
        Debug.Log("panel: " + panel);
        Debug.Log("panel.parent: " + (panel != null ? panel.parent : null));

        center = panel.position;

        float width = panel.rect.width;

        Vector3 baseVector = Vector3.right;
        Vector3 rotatedVector = panel.rotation * baseVector;

        leftOffScreen = center + rotatedVector * -(Screen.width / 2 + width * offScreenMultiplier);
        rightOffScreen = center + rotatedVector * (Screen.width / 2 + width * offScreenMultiplier);

        panel.position = leftOffScreen;

        //패널 비활성화
        panel.gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(" Trigger 들어옴: " + other.name);

        if (!hasShown && other.CompareTag("Player"))
        {
            Debug.Log(" Player 감지됨");

            hasShown = true;
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        //연출시작시 활성화
        panel.gameObject.SetActive(true);

        Debug.Log("DelayedStart 시작");

        yield return new WaitForSeconds(1.5f);

        Debug.Log(" 1.5초 끝, 패널 활성화");

        Time.timeScale = 0;

        StartCoroutine(SlidePanel());
    }

    private IEnumerator SlidePanel()
    {
        Debug.Log("SlidePanel 시작");

        while ((panel.position - center).sqrMagnitude > 0.1f)
        {
            panel.position = Vector3.Lerp(panel.position, center, Time.unscaledDeltaTime * slideSpeed);
            yield return null;
        }

        Debug.Log(" 중앙 도착");

        panel.position = center;

        yield return new WaitForSecondsRealtime(stayTime);

        while ((panel.position - rightOffScreen).sqrMagnitude > 0.1f)
        {
            panel.position = Vector3.Lerp(panel.position, rightOffScreen, Time.unscaledDeltaTime * slideSpeed);
            yield return null;
        }

        panel.position = rightOffScreen;

        Time.timeScale = 1;

        //패널 비활성화
        panel.gameObject.SetActive(false);
    }
}