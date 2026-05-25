using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTransitionManager : MonoBehaviour
{
    public static MapTransitionManager Instance { get; private set; }

    [Header("전환 패널")]
    [SerializeField] private CanvasGroup transitionPanelGroup;

    [Header("페이드 시간")]
    [SerializeField] private float fadeDuration = 0.6f;

    public bool IsTransitioning { get; private set; }
    public bool IsScreenCovered { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        AutoBind();

        InitPanelState();
    }

    private void AutoBind()
    {
        if (transitionPanelGroup != null) return;

        GameObject systemUI = GameObject.Find("System_UI");

        if (systemUI == null)
        {
            Debug.LogWarning("[MapTransitionManager] System_UI를 찾지 못함");
            return;
        }

        Transform panel = UIManager.FindChildRecursive(systemUI.transform, "TransitionPanel");

        if (panel == null)
        {
            Debug.LogWarning("[MapTransitionManager] TransitionPanel을 찾지 못함");
            return;
        }

        transitionPanelGroup = panel.GetComponent<CanvasGroup>();

        if (transitionPanelGroup == null)
        {
            transitionPanelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void InitPanelState()
    {
        if (transitionPanelGroup == null) return;

        transitionPanelGroup.alpha = 0f;
        transitionPanelGroup.interactable = false;
        transitionPanelGroup.blocksRaycasts = false;

        // 다른 UI보다 위에 보이도록 마지막 자식으로 이동
        transitionPanelGroup.transform.SetAsLastSibling();
    }

    public void LoadSceneWithFade(string sceneName, bool fadeFromBlackAfterLoad = true)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[MapTransitionManager] 씬 이름이 비어 있음");
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, fadeFromBlackAfterLoad));
    }

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName, bool fadeFromBlackAfterLoad)
    {
        if (IsTransitioning) yield break;

        IsTransitioning = true;

        SetPlayerInputLocked(true);

        yield return FadeToBlack();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        yield return null;

        if (fadeFromBlackAfterLoad)
        {
            yield return FadeFromBlack();

            SetPlayerInputLocked(false);
            IsTransitioning = false;
        }
        else
        {
            // 보스룸처럼 컷씬이 필요한 경우
            // 화면을 검은 상태로 유지한다.
            IsScreenCovered = true;
            IsTransitioning = false;
        }
    }

    public IEnumerator FadeToBlack()
    {
        AutoBind();

        if (transitionPanelGroup == null)
        {
            Debug.LogWarning("[MapTransitionManager] TransitionPanelGroup이 없음");
            yield break;
        }

        transitionPanelGroup.transform.SetAsLastSibling();

        transitionPanelGroup.blocksRaycasts = true;
        transitionPanelGroup.interactable = true;

        float time = 0f;
        float startAlpha = transitionPanelGroup.alpha;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            transitionPanelGroup.alpha = Mathf.Lerp(startAlpha, 1f, time / fadeDuration);
            yield return null;
        }

        transitionPanelGroup.alpha = 1f;
        IsScreenCovered = true;
    }

    public IEnumerator FadeFromBlack()
    {
        AutoBind();

        if (transitionPanelGroup == null)
        {
            Debug.LogWarning("[MapTransitionManager] TransitionPanelGroup이 없음");
            yield break;
        }

        transitionPanelGroup.transform.SetAsLastSibling();

        transitionPanelGroup.blocksRaycasts = true;
        transitionPanelGroup.interactable = true;

        float time = 0f;
        float startAlpha = transitionPanelGroup.alpha;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            transitionPanelGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }

        transitionPanelGroup.alpha = 0f;
        transitionPanelGroup.blocksRaycasts = false;
        transitionPanelGroup.interactable = false;

        IsScreenCovered = false;
    }

    public IEnumerator FadeFromBlackAndUnlockPlayer()
    {
        yield return FadeFromBlack();

        SetPlayerInputLocked(false);
    }

    public void SetPlayerInputLocked(bool locked)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetSystemInputLocked(locked);
        }
    }
    public void BringTransitionToFront()
    {
        if (transitionPanelGroup != null)
        {
            transitionPanelGroup.transform.SetAsLastSibling();
        }
    }
}