using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CrosshairSingleton : MonoBehaviour
{
    public static CrosshairSingleton Instance;

    [SerializeField] private GameObject crosshairPrefab;
    private bool isAttaching = false;
    private GameObject crosshairObj;
    public Transform Crosshair => crosshairObj != null ? crosshairObj.transform : null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(AttachRoutine());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AttachRoutine());
    }

    private IEnumerator AttachRoutine()
    {
        if (isAttaching)
            yield break;

        isAttaching = true;

        if (crosshairObj != null)
        {
            Destroy(crosshairObj);
            crosshairObj = null;
        }

        crosshairObj = Instantiate(crosshairPrefab);
        DontDestroyOnLoad(crosshairObj);

        GameObject canvasObj = null;
        int tryCount = 120;

        while (canvasObj == null && tryCount-- > 0)
        {
            canvasObj = GameObject.Find("Canvas_Crosshair");
            yield return null;
        }

        if (canvasObj == null)
        {
            Debug.LogError("[Crosshair] Canvas 없음");
            isAttaching = false;
            yield break;
        }

        crosshairObj.transform.SetParent(canvasObj.transform, false);
        crosshairObj.SetActive(true);

        Debug.Log($"[Crosshair] 성공 ({canvasObj.scene.name})");

        isAttaching = false;
    }
}