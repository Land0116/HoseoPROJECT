using UnityEngine;
using System.Collections;

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

    private void Start()
    {
        center = panel.parent.GetComponent<RectTransform>().position;

        float width = panel.rect.width;

       
        Vector3 baseVector = Vector3.right;
        Vector3 rotatedVector = panel.rotation * baseVector;

        leftOffScreen = center + rotatedVector * -(Screen.width / 2 + width * offScreenMultiplier);
        rightOffScreen = center + rotatedVector * (Screen.width / 2 + width * offScreenMultiplier);

        panel.position = leftOffScreen;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasShown && other.CompareTag("Player"))
        {
            hasShown = true;

            
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        
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
    }
}