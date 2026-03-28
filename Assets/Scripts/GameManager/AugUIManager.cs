using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class AugUIManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static AugUIManager instance;

    [Header("전체 증강 데이터")]
    // 게임 내에서 사용할 전체 증강 데이터 목록
    public AugmentationSystem[] augmentationDatabase;

    [Header("UI 버튼 3개")]
    // 증강 선택용 버튼 3개
    public AugButton[] uiButtons;

    [Header("증강 선택 패널")]
    // 증강 선택창 전체 패널
    public GameObject uiPanel;

    [Header("보유 증강 아이콘 슬롯")]
    // 현재 런에서 먹은 증강을 보여줄 아이콘 슬롯들
    [SerializeField] private Image[] slotImages;

    [Header("게임이 시작될 때 UI 설정")]
    // 게임 시작하자마자 테스트용으로 증강 UI를 띄울지 여부
    [SerializeField] public bool showOnPlayForTest = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 시작 시 보유 증강 아이콘 슬롯 초기화
        ClearOwnedAugmentUI();

        // 테스트용으로 시작 직후 증강 UI 자동 오픈
        if (showOnPlayForTest)
        {
            StartCoroutine(ShowAugmentationOnStartRoutine());
            showOnPlayForTest = false;
        }
    }

    private IEnumerator ShowAugmentationOnStartRoutine()
    {
        // 다른 매니저들의 Awake / Start가 먼저 끝날 수 있게 1프레임 대기
        yield return null;

        // AugmentRunManager가 생성될 때까지 기다림
        while (AugmentRunManager.Instance == null)
        {
            yield return null;
        }

        // 혹시 이미 보유 중인 증강이 있다면 아이콘 UI 먼저 갱신
        RefreshOwnedAugmentUI();

        // 바로 증강 선택창 띄우기
        ShowAugmentation();
    }

    public bool CanShowAugmentation()
    {
        // 런 매니저가 없으면 증강 UI를 띄울 수 없음
        if (AugmentRunManager.Instance == null) return false;

        // 이미 최대 개수만큼 증강을 먹었다면 더 이상 띄울 수 없음
        if (!AugmentRunManager.Instance.CanPickMore) return false;

        // 아직 해금되어 있고, 아직 먹지 않은 증강이 하나라도 있어야 함
        return augmentationDatabase.Any(aug =>
            aug != null &&
            aug.isUnlocked &&
            !AugmentRunManager.Instance.HasAugment(aug));
    }

    public void ShowAugmentation()
    {
        // 증강 선택이 가능한 상태가 아니라면 패널 닫고 종료
        if (!CanShowAugmentation())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);

            return;
        }

        // 게임 일시정지
        Time.timeScale = 0f;

        // 증강 선택 패널 열기
        if (uiPanel != null)
            uiPanel.SetActive(true);

        // 3개 랜덤 선택
        List<AugmentationSystem> selectedAugments = GetRandomAugments(3);

        // 버튼에 증강 데이터 연결
        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (i < selectedAugments.Count)
            {
                uiButtons[i].gameObject.SetActive(true);
                uiButtons[i].Setup(selectedAugments[i], this);
            }
            else
            {
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private List<AugmentationSystem> GetRandomAugments(int count)
    {
        // 최종적으로 뽑혀서 반환될 증강 리스트
        List<AugmentationSystem> result = new List<AugmentationSystem>();
        
        List<AugmentationSystem> available = augmentationDatabase
            .Where(aug => aug != null &&
                          aug.isUnlocked &&
                          !AugmentRunManager.Instance.HasAugment(aug))
            .ToList();

        // count 개수만큼 뽑되, available이 비면 중단
        while (result.Count < count && available.Count > 0)
        {
            // 가중치 기반으로 우선 뽑고 싶은 타입 결정
            AugmentationSystem.AugmentationType targetType = GetWeightedType();

            // 해당 타입만 따로 모음
            List<AugmentationSystem> typePool = available
                .Where(x => x.augmentationType == targetType)
                .ToList();

            // 같은 타입이 2개 이미 뽑혔으면
            // 3번째도 같은 타입이 나오지 않게 막음
            if (result.Count >= 2 &&
                result[0].augmentationType == result[1].augmentationType)
            {
                typePool = available
                    .Where(x => x.augmentationType != result[0].augmentationType)
                    .ToList();
            }

            // 만약 해당 타입 풀이 비어 있으면 전체 available에서 뽑음
            List<AugmentationSystem> finalPool = typePool.Count > 0 ? typePool : available;

            // 랜덤 1개 선택
            int rand = Random.Range(0, finalPool.Count);
            AugmentationSystem picked = finalPool[rand];

            // 결과에 추가하고, 중복 방지를 위해 available에서는 제거
            result.Add(picked);
            available.Remove(picked);
        }

        return result;
    }

    private AugmentationSystem.AugmentationType GetWeightedType()
    {
        // 0 ~ 100 사이 랜덤값
        float rand = Random.Range(0f, 100f);

        // 공격 40%
        if (rand < 40f) return AugmentationSystem.AugmentationType.Atk;

        // 방어 30%
        if (rand < 70f) return AugmentationSystem.AugmentationType.Dfs;

        // 유틸 30%
        return AugmentationSystem.AugmentationType.Util;
    }

    public void SelectAugmentation(AugmentationSystem selectedData)
    {
        // 예외처리
        if (selectedData == null) return;
        if (AugmentRunManager.Instance == null) return;

        // 런 매니저에 증강 추가 시도
        // 이미 먹었거나 최대 개수면 false
        bool added = AugmentRunManager.Instance.TryAddAugment(selectedData);

        // 추가 실패 시 종료
        if (!added) return;

        // 플레이어 스탯 재계산
        // 증강 효과 적용을 여기서 다시 반영
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.RebuildPlayerStats();
        }

        // 보유 증강 아이콘 슬롯 갱신
        RefreshOwnedAugmentUI();

        // 게임 재개
        Time.timeScale = 1f;

        // 선택 패널 닫기
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    public void RefreshOwnedAugmentUI()
    {
        // 먼저 슬롯 비우기
        ClearOwnedAugmentUI();

        // 런 매니저가 없으면 종료
        if (AugmentRunManager.Instance == null) return;
        if (slotImages == null) return;

        // 보유 중인 증강 목록 가져오기
        IReadOnlyList<AugmentationSystem> ownedAugments = AugmentRunManager.Instance.OwnedAugments;

        // 슬롯 개수만큼 아이콘 채우기
        for (int i = 0; i < ownedAugments.Count && i < slotImages.Length; i++)
        {
            if (ownedAugments[i] == null) continue;
            if (slotImages[i] == null) continue;

            // 슬롯 오브젝트 활성화
            slotImages[i].gameObject.SetActive(true);

            // 아이콘 넣기
            slotImages[i].sprite = ownedAugments[i].icon;

            // 아이콘이 있을 때만 Image 활성화
            slotImages[i].enabled = ownedAugments[i].icon != null;
        }
    }

    private void ClearOwnedAugmentUI()
    {
        // 슬롯 배열이 비어있으면 종료
        if (slotImages == null) return;

        // 모든 슬롯 초기화
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            slotImages[i].sprite = null;
            slotImages[i].enabled = false;
            slotImages[i].gameObject.SetActive(false);
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
        // 씬 이동 후 멈춘 시간 복구
        Time.timeScale = 1f;

        // 보유 증강 UI 갱신
        RefreshOwnedAugmentUI();

        // 다음 씬 시작 시 증강창을 띄워야 하면 여기서 직접 실행
        if (showOnPlayForTest)
        {
            StartCoroutine(ShowAugmentationOnStartRoutine());
            showOnPlayForTest = false;
        }
    }
    
    public void RequestShowOnNextScene() //UI 감지
    {
        showOnPlayForTest = true;
    }

    public void ResetUIStateForRestart()
    {
        Time.timeScale = 1f;

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        ClearOwnedAugmentUI();
    }
    
    public void BindAugUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform augPanelRoot = systemUIRoot.transform.Find("AugmentationUIPanel");
        if (augPanelRoot != null)
        {
            uiPanel = augPanelRoot.gameObject;

            uiButtons = augPanelRoot.GetComponentsInChildren<AugButton>(true);
        }
    }
}