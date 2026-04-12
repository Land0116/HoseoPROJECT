using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class StageClear : MonoBehaviour
{
    public static StageClear Instance;

    [Header("스테이지 씬 이름 배열 (순서대로 넣기)")]
    [SerializeField] private string[] stageSceneNames;

    [Header("몬스터 전멸 시 자동 클리어 여부")]
    [SerializeField] private bool autoClearWhenNoMonster = true;

    [Header("출구에 들어올 플레이어 태그")]
    [SerializeField] private string playerTag = "Player";
    [Header("몬스터 태그")]
    [SerializeField] private string monsterTag = "Monster";

    [Header("클리어 후 다음 씬 이동까지 딜레이")]
    [SerializeField] private float clearDelay = 1.0f;

    // 현재 씬이 배열에서 몇 번째인지 저장
    private int currentSceneIndex = -1;

    // 중복 클리어 방지
    private bool isStageCleared = false;
    // 이미 다음 씬으로 넘어가는 중인지
    [SerializeField] private bool isLoadingNextScene = false;

    // 출구 충돌체
    [SerializeField] private CompositeCollider2D exitCompositeCollider;
    [SerializeField] private Rigidbody2D exitRigidbody;

    // 씬 이동 코루틴 저장용
    private Coroutine clearRoutine;

     private void Awake()
    {
        // 싱글톤 처리
        if (Instance != null && Instance != this)
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
        // 첫 시작 씬도 직접 한 번 바인딩
        BindSceneMoveCollider();
        currentSceneIndex = GetSceneIndex(SceneManager.GetActiveScene().name);
        ResetStageClearState();

        Debug.Log("현재 씬 인덱스 : " + currentSceneIndex + " / 씬 이름 : " + SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 바뀔 때마다 새 SceneMoveColl 다시 찾기
        BindSceneMoveCollider();

        // 씬 인덱스 다시 계산
        currentSceneIndex = GetSceneIndex(scene.name);

        // 스테이지 상태 초기화
        ResetStageClearState();

        Debug.Log("씬 로드 완료 / 현재 씬 인덱스 : " + currentSceneIndex + " / 씬 이름 : " + scene.name);
    }

    private void BindSceneMoveCollider()
    {
        GameObject moveCollObj = GameObject.Find("SceneMoveColl");

        if (moveCollObj == null)
        {
            exitCompositeCollider = null;
            exitRigidbody = null;
            Debug.LogError("SceneMoveColl 오브젝트를 찾지 못함");
            return;
        }

        exitCompositeCollider = moveCollObj.GetComponent<CompositeCollider2D>();
        exitRigidbody = moveCollObj.GetComponent<Rigidbody2D>();

        if (exitCompositeCollider == null)
        {
            Debug.LogError("SceneMoveColl 오브젝트에 CompositeCollider2D가 없음");
            return;
        }

        if (exitRigidbody == null)
        {
            Debug.LogError("SceneMoveColl 오브젝트에 Rigidbody2D가 없음");
            return;
        }

        // 출구는 움직이지 않으므로 Static
        exitRigidbody.bodyType = RigidbodyType2D.Static;

        // 기본은 막힌 상태
        exitCompositeCollider.isTrigger = false;
    }


    private void Update()
    {
        // 이미 클리어됐거나 씬 이동 중이면 검사 안 함
        if (isStageCleared) return;
        if (isLoadingNextScene) return;

        // 자동 클리어 검사 꺼져 있으면 검사 안 함
        if (!autoClearWhenNoMonster) return;

        GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);

        //Debug.Log("남은 몬스터 수 : " + monsters.Length);

        // 몬스터가 0마리면 출구 열기
        if (monsters.Length == 0)
        {
            autoClearWhenNoMonster = true;
            ClearStage();
        }
    }

    /// <summary>
    /// 씬 시작 시 상태 초기화
    /// </summary>
    private void ResetStageClearState()
    {
        isStageCleared = false;
        isLoadingNextScene = false;

        if (exitCompositeCollider != null)
        {
            exitCompositeCollider.isTrigger = false;
        }
    }

    /// <summary>
    /// 현재 씬 이름이 배열에서 몇 번째인지 찾는 함수
    /// 없으면 -1 반환
    /// </summary>
    private int GetSceneIndex(string sceneName)
    {
        if (stageSceneNames == null || stageSceneNames.Length == 0)
            return -1;

        for (int i = 0; i < stageSceneNames.Length; i++)
        {
            if (stageSceneNames[i] == sceneName)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 현재 씬 다음 씬 이름 반환
    /// 마지막 씬이면 null 반환
    /// </summary>
    private string GetNextSceneName()
    {
        if (currentSceneIndex < 0)
            return null;

        int nextIndex = currentSceneIndex + 1;

        if (nextIndex >= stageSceneNames.Length)
            return null;

        return stageSceneNames[nextIndex];
    }

    /// <summary>
    /// 스테이지 클리어 처리
    /// 몬스터를 다 잡으면 출구를 Trigger로 열어줌
    /// </summary>
    public void ClearStage()
    {
        if (isStageCleared) return;

        isStageCleared = true;

        if (exitCompositeCollider != null)
        {
            exitCompositeCollider.isTrigger = true;
        }

        Debug.Log("스테이지 클리어 - 출구 Trigger 활성화");
    }
    
    /// <summary>
    /// 다음 씬 이동 시도
    /// </summary>
    public void TryMoveNextScene(Collider2D other)
    {
        if (other == null) return;
        if (!isStageCleared) return;
        if (isLoadingNextScene) return;
        if (!other.CompareTag(playerTag)) return;

        string nextSceneName = GetNextSceneName();

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("다음 씬이 없음. 마지막 스테이지일 가능성 있음.");
            return;
        }

        isLoadingNextScene = true;
        StartCoroutine(LoadNextSceneRoutine(nextSceneName));
    }

    /// <summary>
    /// 실제 다음 씬 이동 코루틴
    /// </summary>
    private IEnumerator LoadNextSceneRoutine(string nextSceneName)
    {
        // 혹시 멈춰있으면 해제
        Time.timeScale = 1f;

        // 다음 씬 진입 시 증강 UI 띄우기 예약
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestStageEntryUI();
        }

        yield return new WaitForSeconds(clearDelay);

        SceneManager.LoadScene(nextSceneName);
    }
}