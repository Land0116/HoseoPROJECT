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

    [Header("몬스터 태그")]
    [SerializeField] private string monsterTag = "Monster";

    [Header("클리어 후 다음 씬 이동까지 딜레이")]
    [SerializeField] private float clearDelay = 1.0f;

    // 현재 씬이 배열에서 몇 번째인지 저장
    private int currentSceneIndex = -1;

    // 중복 클리어 방지
    private bool isStageCleared = false;

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

    private void Update()
    {
        // 이미 클리어 처리 중이면 더 이상 검사 안 함
        if (isStageCleared) return;

        // 테스트용:
        // N 키를 누르면 자동 클리어 검사 활성화
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            autoClearWhenNoMonster = true;
        }

        // 자동 클리어 옵션이 켜져 있으면 몬스터 수 검사
        if (autoClearWhenNoMonster)
        {
            GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);

            Debug.Log("남은 몬스터 수 : " + monsters.Length);

            // 몬스터가 0마리면 스테이지 클리어
            if (monsters.Length == 0)
            {
                ClearStage();
            }
        }
    }

    private void OnEnable()
    {
        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 다시 클리어 가능 상태로 초기화
        isStageCleared = false;

        // 현재 씬이 배열에서 몇 번째인지 계산
        currentSceneIndex = GetSceneIndex(scene.name);

        Debug.Log("현재 씬 인덱스 : " + currentSceneIndex + " / 씬 이름 : " + scene.name);
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
        // 현재 씬이 배열에 없으면 진행 불가
        if (currentSceneIndex < 0)
            return null;

        int nextIndex = currentSceneIndex + 1;

        // 마지막 씬이면 다음 씬 없음
        if (nextIndex >= stageSceneNames.Length)
            return null;

        return stageSceneNames[nextIndex];
    }

    /// <summary>
    /// 스테이지 클리어 처리 시작
    /// 바로 씬 이동하지 않고 딜레이 후 이동
    /// </summary>
    public void ClearStage()
    {
        // 중복 실행 방지
        if (isStageCleared) return;
        isStageCleared = true;

        // 이미 코루틴이 실행 중이면 정리
        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
        }

        clearRoutine = StartCoroutine(ClearStageRoutine());
    }

    /// <summary>
    /// 실제 스테이지 클리어 처리 코루틴
    /// </summary>
    private IEnumerator ClearStageRoutine()
    {
        // 혹시 일시정지 상태였으면 해제
        Time.timeScale = 1f;

        // 다음 씬 진입 시 증강 UI가 뜨도록 예약
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestStageEntryUI();
        }

        // 자동 클리어 검사 끔
        autoClearWhenNoMonster = false;

        // 다음 씬 이름 가져오기
        string nextSceneName = GetNextSceneName();

        // 다음 씬이 없으면 종료
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("다음 씬이 없음. 현재 배열의 마지막 스테이지일 가능성 있음.");
            yield break;
        }

        // 테스트용 딜레이
        yield return new WaitForSeconds(clearDelay);

        // 다음 씬 로드
        SceneManager.LoadScene(nextSceneName);
    }
}