using UnityEngine;
using UnityEngine.SceneManagement;

public class udstSpawner : MonoBehaviour
{
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private RectTransform spawnArea;

    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField] private float spawnRange = 350f;

    private float timer;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (Transform child in spawnArea)
        {
            Destroy(child.gameObject);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnDust();
        }
    }

    public void SpawnDust()
    {
        GameObject obj = Instantiate(dustPrefab, spawnArea);

        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchoredPosition = new Vector2(
            Random.Range(-spawnRange, spawnRange),
            Random.Range(-spawnRange, spawnRange)
        );

        obj.GetComponent<DustImage>().Init();
    }
}