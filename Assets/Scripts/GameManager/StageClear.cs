using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class StageClear : MonoBehaviour
{
    public static StageClear Instance;
    [Header("다음 씬 이름")]
    [SerializeField] private string nextSceneName = "Tutorial_Stage";

    [Header("몬스터 전멸 시 자동 클리어 여부")]
    [SerializeField] private bool autoClearWhenNoMonster;

    [Header("몬스터 태그")]
    [SerializeField] private string monsterTag = "Monster";

    // 중복 클리어 방지용
    private bool isStageCleared = false;


    private void Awake()
    {
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
        // 이미 클리어 처리됐으면 더 이상 실행 안 함
        if (isStageCleared) return;

        // New Input System 방식
        // N 키를 누른 프레임에만 클리어
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            autoClearWhenNoMonster = true;
            //return;
        }

        // 옵션이 켜져 있으면 몬스터가 0마리일 때 자동 클리어
        if (autoClearWhenNoMonster)
        {
            GameObject[] monsters = GameObject.FindGameObjectsWithTag(monsterTag);
            
            Debug.Log(monsters.Length);
            if (monsters.Length == 0)
            {
                ClearStage();
            }
        }
    }
    
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
        isStageCleared = false;
    }
    
    public void ClearStage()
    {
        // 중복 실행 방지
        if (isStageCleared) return;
        isStageCleared = true;

        // 혹시 시간 멈춘 상태면 복구
        Time.timeScale = 1f;

        // 다음 씬 시작 시 증강창 다시 띄우고 싶으면 true
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.showOnPlayForTest = true;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestStageEntryUI();
        }

        autoClearWhenNoMonster = false;

        // 다음 씬 로드
        SceneManager.LoadScene(nextSceneName);
    }
}