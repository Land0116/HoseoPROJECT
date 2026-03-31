using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        //�г� ��Ȱ��ȭ
        panel.gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(" Trigger ����: " + other.name);

        if (!hasShown && other.CompareTag("Player"))
        {
            Debug.Log(" Player ������");

            hasShown = true;
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        panel.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        Time.timeScale = 0;

        StartCoroutine(SlidePanel());
    }

    private IEnumerator SlidePanel()
    {

        while ((panel.position - center).sqrMagnitude > 0.1f)
        {
            panel.position = Vector3.Lerp(panel.position, center, Time.unscaledDeltaTime * slideSpeed);
            yield return null;
        }
        
        panel.position = center;

        yield return new WaitForSecondsRealtime(stayTime);

        while ((panel.position - rightOffScreen).sqrMagnitude > 0.1f)
        {
            panel.position = Vector3.Lerp(panel.position, rightOffScreen, Time.unscaledDeltaTime * slideSpeed);
            yield return null;
        }

        panel.position = rightOffScreen;

        Time.timeScale = 1;
        
        panel.gameObject.SetActive(false);
        
    }
}