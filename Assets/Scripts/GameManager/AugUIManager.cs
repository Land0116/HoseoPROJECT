using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class AugUIManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static AugUIManager instance;

    [Header("전체 증강 데이터")]
    // 게임 내에서 사용할 전체 증강 데이터 목록
    public AugmentationSystem[] augmentationDatabase;
    
    [Header("현재 화면에 표시 중인 증강 3개")]
    [SerializeField] private List<AugmentationSystem> currentShownAugments = new List<AugmentationSystem>();
    [Header("각 칸 reroll 사용 여부")]
    [SerializeField] private List<bool> rerollUsedStates = new List<bool>();

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
    [SerializeField] public bool showOnPlayForTest;

    private void Awake()
    {
        // 싱글톤 패턴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        // 시작 시 슬롯 비워두기
        ClearOwnedAugmentUI();
    }

    /// <summary>
    /// 다음 게임 씬에서 증강창을 띄우기 위한 예약 플래그
    /// </summary>
    public void RequestShowOnNextScene()
    {
        showOnPlayForTest = true;
    }

    /// <summary>
    /// 예약되어 있으면 증강창 오픈 시도
    /// 이 함수는 UIManager가 sceneLoaded 후 호출
    /// </summary>
    public void TryOpenReservedAugmentation()
    {
        if (!showOnPlayForTest) return;

        StartCoroutine(ShowAugmentationOnStartRoutine());
        showOnPlayForTest = false;
    }

    /// <summary>
    /// 다른 매니저 / 플레이어 / 런 매니저가 먼저 준비될 수 있게 한 프레임 대기 후 증강창 열기
    /// </summary>
    public IEnumerator ShowAugmentationOnStartRoutine()
    {
        // 한 프레임 대기
        yield return null;

        // AugmentRunManager가 아직 없으면 생길 때까지 대기
        while (AugmentRunManager.Instance == null)
        {
            yield return null;
        }

        // 현재 보유 증강 UI 먼저 갱신
        RefreshOwnedAugmentUI();

        // 증강창 열기
        ShowAugmentation();
    }

    /// <summary>
    /// 현재 씬에서 증강창을 열 수 있는지 판단
    /// </summary>
    public bool CanShowAugmentation()
    {
        // 런 매니저가 없으면 불가
        if (AugmentRunManager.Instance == null) return false;

        // 이미 최대 증강 수를 다 먹었다면 불가
        if (!AugmentRunManager.Instance.CanPickMore) return false;

        // 아직 선택 가능한 증강이 하나라도 있어야 함
        return augmentationDatabase.Any(aug =>
            aug != null &&
            aug.isUnlocked &&
            !AugmentRunManager.Instance.HasAugment(aug));
    }

    /// <summary>
    /// 증강 선택 패널 열기
    /// </summary>
    public void ShowAugmentation()
    {
        // Main 씬에서는 증강창 금지
        if (!CanOpenAugUIInCurrentScene())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);

            return;
        }

        // 선택 가능한 증강이 없으면 종료
        if (!CanShowAugmentation())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);

            return;
        }

        // 게임 멈춤
        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetPause(true);
            PlayerController.Instance.SetControl(false);
        }

        // UI 패널 켜기
        if (uiPanel != null)
            uiPanel.SetActive(true);
        else
            return;

        // 현재 표시할 3개 저장
        currentShownAugments = GetRandomAugments(3);

        // 각 칸 reroll 사용 여부 초기화
        rerollUsedStates.Clear();
        for (int i = 0; i < currentShownAugments.Count; i++)
        {
            rerollUsedStates.Add(false);
        }

        // 버튼 반영
        ApplyCurrentAugmentsToButtons();
    }
    
    
    private void ApplyCurrentAugmentsToButtons()
    {
        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (i < currentShownAugments.Count && currentShownAugments[i] != null)
            {
                uiButtons[i].gameObject.SetActive(true);

                bool canReroll = i < rerollUsedStates.Count && !rerollUsedStates[i];
                uiButtons[i].Setup(currentShownAugments[i], this, i, canReroll);
            }
            else
            {
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    public void RerollAugmentation(int slotIndex)
    {
        // 인덱스 예외 방지
        if (slotIndex < 0 || slotIndex >= currentShownAugments.Count) return;
        if (slotIndex >= rerollUsedStates.Count) return;
        if (AugmentRunManager.Instance == null) return;

        // 이미 이 칸은 reroll 사용했으면 종료
        if (rerollUsedStates[slotIndex]) return;

        AugmentationSystem currentAug = currentShownAugments[slotIndex];
        if (currentAug == null) return;

        // 제외할 증강 목록 구성
        HashSet<AugmentationSystem> excludedAugments = new HashSet<AugmentationSystem>();

        // 현재 칸에 있는 기존 증강 제외
        excludedAugments.Add(currentAug);

        // 다른 칸에 이미 떠 있는 증강도 제외
        for (int i = 0; i < currentShownAugments.Count; i++)
        {
            if (i == slotIndex) continue;
            if (currentShownAugments[i] == null) continue;

            excludedAugments.Add(currentShownAugments[i]);
        }

        // 새로운 증강 1개 뽑기
        AugmentationSystem rerolledAug = GetRandomAugmentForSingleSlot(excludedAugments);

        // 후보가 없으면 종료
        if (rerolledAug == null)
        {
            Debug.Log("리롤 가능한 다른 증강이 없음");
            return;
        }

        // 해당 칸 증강 교체
        currentShownAugments[slotIndex] = rerolledAug;

        // 이 칸 reroll 사용 처리
        rerollUsedStates[slotIndex] = true;

        // 버튼 다시 세팅 - 이번엔 reroll 불가 상태로 세팅
        uiButtons[slotIndex].Setup(rerolledAug, this, slotIndex, false);
    }
    
    private AugmentationSystem GetRandomAugmentForSingleSlot(HashSet<AugmentationSystem> excludedAugments)
    {
        List<AugmentationSystem> available = augmentationDatabase
            .Where(aug =>
                aug != null &&
                aug.isUnlocked &&
                !AugmentRunManager.Instance.HasAugment(aug) &&
                !excludedAugments.Contains(aug))
            .ToList();

        if (available.Count == 0)
            return null;

        // 기존 가중치 로직 재사용
        AugmentationSystem.AugmentationType targetType = GetWeightedType();

        List<AugmentationSystem> typePool = available
            .Where(x => x.augmentationType == targetType)
            .ToList();

        List<AugmentationSystem> finalPool = typePool.Count > 0 ? typePool : available;

        int rand = Random.Range(0, finalPool.Count);
        return finalPool[rand];
    }
    
    /// <summary>
    /// 현재 증강 UI가 켜져 있는지
    /// </summary>
    public bool IsAugmentationVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    /// <summary>
    /// ESC로 잠시 숨길 때 사용
    /// 다시 보여줄 때는 기존 선택지가 그대로 유지됨
    /// </summary>
    public void HideCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    /// <summary>
    /// ESC 패널에서 돌아왔을 때 증강창 복원
    /// 기존 선택지를 다시 그대로 보여줌
    /// </summary>
    public void RestoreCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(true);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }

    /// <summary>
    /// 증강 3개 랜덤 뽑기
    /// 같은 런에서 이미 보유 중인 건 제외
    /// 타입 가중치 적용
    /// </summary>
    private List<AugmentationSystem> GetRandomAugments(int count)
    {
        List<AugmentationSystem> result = new List<AugmentationSystem>();

        // 사용 가능한 증강 목록
        List<AugmentationSystem> available = augmentationDatabase
            .Where(aug => aug != null &&
                          aug.isUnlocked &&
                          !AugmentRunManager.Instance.HasAugment(aug))
            .ToList();

        // count개 뽑되, available이 다 떨어지면 중단
        while (result.Count < count && available.Count > 0)
        {
            // 가중치로 원하는 타입 먼저 정함
            AugmentationSystem.AugmentationType targetType = GetWeightedType();

            // 해당 타입만 추리기
            List<AugmentationSystem> typePool = available
                .Where(x => x.augmentationType == targetType)
                .ToList();

            // 앞의 2개가 같은 타입이면 3번째는 다른 타입 우선
            if (result.Count >= 2 &&
                result[0].augmentationType == result[1].augmentationType)
            {
                typePool = available
                    .Where(x => x.augmentationType != result[0].augmentationType)
                    .ToList();
            }

            // 타입 풀이 비었으면 전체 available에서 뽑기
            List<AugmentationSystem> finalPool = typePool.Count > 0 ? typePool : available;

            // 랜덤 선택
            int rand = Random.Range(0, finalPool.Count);
            AugmentationSystem picked = finalPool[rand];

            // 결과에 추가하고 available에서 제거
            result.Add(picked);
            available.Remove(picked);
        }

        return result;
    }

    /// <summary>
    /// 타입별 가중치
    /// 공격 40 / 방어 30 / 유틸 30
    /// </summary>
    private AugmentationSystem.AugmentationType GetWeightedType()
    {
        float rand = Random.Range(0f, 100f);

        if (rand < 40f) return AugmentationSystem.AugmentationType.Atk;
        if (rand < 70f) return AugmentationSystem.AugmentationType.Dfs;
        return AugmentationSystem.AugmentationType.Util;
    }

    /// <summary>
    /// 증강 선택 확정
    /// </summary>
    public void SelectAugmentation(AugmentationSystem selectedData)
    {
        // 잘못된 선택 방지
        if (selectedData == null) return;
        if (AugmentRunManager.Instance == null) return;

        // 런 매니저에 증강 추가
        bool added = AugmentRunManager.Instance.TryAddAugment(selectedData);
        if (!added) return;

        // 플레이어 스탯 재계산
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.RebuildPlayerStats();
        }

        // 보유 증강 슬롯 UI 갱신
        RefreshOwnedAugmentUI();

        // HUD 다시 표시
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowPlayerHUD();
        }

        // 여기서 바로 게임 재개하지 말고,
        // 클릭을 뗀 뒤 닫는 코루틴으로 넘김
        StartCoroutine(CloseAugmentationAfterMouseRelease());
    }
    private IEnumerator CloseAugmentationAfterMouseRelease()
    {
        // 지금 누른 클릭이 완전히 끝날 때까지 대기
        while (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            yield return null;
        }

        // 혹시 남아있는 UI 클릭 처리 한 프레임 더 넘김
        yield return null;

        // 증강 패널 닫기
        if (uiPanel != null)
            uiPanel.SetActive(false);

        // 게임 재개
        Time.timeScale = 1f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetPause(false);
            PlayerController.Instance.SetControl(true);
        }
    }
    /// <summary>
    /// 보유 중인 증강 아이콘 슬롯 갱신
    /// </summary>
    public void RefreshOwnedAugmentUI()
    {
        // 먼저 모두 비움
        ClearOwnedAugmentUI();

        if (AugmentRunManager.Instance == null) return;
        if (slotImages == null) return;

        IReadOnlyList<AugmentationSystem> ownedAugments = AugmentRunManager.Instance.OwnedAugments;

        for (int i = 0; i < ownedAugments.Count && i < slotImages.Length; i++)
        {
            if (ownedAugments[i] == null) continue;
            if (slotImages[i] == null) continue;

            slotImages[i].gameObject.SetActive(true);
            slotImages[i].sprite = ownedAugments[i].icon;
            slotImages[i].enabled = ownedAugments[i].icon != null;
        }
    }

    /// <summary>
    /// 보유 아이콘 슬롯 전부 비우기
    /// </summary>
    private void ClearOwnedAugmentUI()
    {
        if (slotImages == null) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            slotImages[i].sprite = null;
            slotImages[i].enabled = false;
            slotImages[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Main 씬에서는 증강 UI 금지
    /// </summary>
    private bool CanOpenAugUIInCurrentScene()
    {
        return SceneManager.GetActiveScene().name != "Main";
    }

    /// <summary>
    /// 재시작 / 메인 복귀 시 UI 상태 정리
    /// </summary>
    public void ResetUIStateForRestart()
    {
        Time.timeScale = 1f;

        showOnPlayForTest = false;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        ClearOwnedAugmentUI();

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(false);
    }

    /// <summary>
    /// System_UI 아래에서 증강 UI 다시 연결
    /// </summary>
    public void BindAugUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        // 증강 패널 찾기
        Transform augPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugmentationUIPanel");
        if (augPanelRoot != null)
        {
            uiPanel = augPanelRoot.gameObject;

            // 하위 AugButton 자동 수집
            uiButtons = augPanelRoot.GetComponentsInChildren<AugButton>(true);
        }

        // 보유 증강 슬롯 루트 찾기
        Transform slotRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugUIPanel");
        if (slotRoot != null)
        {
            // Slot 이름이 들어간 Image만 슬롯으로 사용
            slotImages = slotRoot
                .GetComponentsInChildren<Image>(true)
                .Where(x => x.name.Contains("Slot"))
                .ToArray();
        }

        // 바인딩 직후 현재 런 상태 반영
        RefreshOwnedAugmentUI();
    }
}