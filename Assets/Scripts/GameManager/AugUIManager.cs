using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AugUIManager : MonoBehaviour
{
    // 어디서든 쉽게 접근하려고 싱글톤으로 사용
    public static AugUIManager instance;

    [Header("전체 증강 데이터")]
    // ScriptableObject로 만들어 둔 모든 증강 데이터를 넣는 배열
    // 예: 총알 공격력 증가, 이동속도 증가, 최대체력 증가 등
    public AugmentationSystem[] augmentationDatabase;

    [Header("UI 버튼 3개")]
    // 씬에 배치한 증강 선택 버튼 3개
    // 인스펙터에서 순서대로 넣어주면 됨
    public AugButton[] uiButtons;

    [Header("증강 선택 패널")]
    // 증강창 전체 패널
    // ShowAugmentation() 호출 시 활성화 / 선택 후 비활성화
    public GameObject uiPanel;

    // 이미 먹은 증강을 저장하는 리스트
    // 한 번 선택한 증강이 다시 나오지 않도록 막는 용도
    private List<AugmentationSystem> ownedAugments = new List<AugmentationSystem>();

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 증강 선택 UI를 띄우는 함수
    /// 외부에서 레벨업, 웨이브 종료, 보상 획득 등의 타이밍에 호출하면 됨
    /// </summary>
    public void ShowAugmentation()
    {
        // 게임 멈춤
        Time.timeScale = 0f;

        // UI 패널 활성화
        uiPanel.SetActive(true);

        // 중복 없이 랜덤 증강 3개 뽑기
        List<AugmentationSystem> selectedAugments = GetRandomAugments(3);

        // 뽑은 증강 데이터를 버튼에 넣어줌
        for (int i = 0; i < uiButtons.Length; i++)
        {
            // 뽑힌 증강 개수보다 버튼 개수가 많을 수 있으니 체크
            if (i < selectedAugments.Count)
            {
                uiButtons[i].gameObject.SetActive(true);

                // 버튼 하나에 증강 데이터 하나씩 세팅
                uiButtons[i].Setup(selectedAugments[i], this);
            }
            else
            {
                // 보여줄 증강이 없으면 버튼 비활성화
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 랜덤 증강 count개를 뽑는 함수
    /// 조건:
    /// 1. 이미 먹은 증강은 제외
    /// 2. 같은 UI 안에서 중복 금지
    /// 3. 공격형이 있으면 먼저 우선적으로 채움
    /// 4. 공격형이 부족하거나 다 먹었으면 수비/유틸로 채움
    /// </summary>
    private List<AugmentationSystem> GetRandomAugments(int count)
    {
        // 최종적으로 UI에 보여줄 증강 리스트
        List<AugmentationSystem> result = new List<AugmentationSystem>();

        // 아직 먹지 않은 공격형 증강만 따로 뽑음
        List<AugmentationSystem> attackList = augmentationDatabase
            .Where(aug => aug.augmentationType == AugmentationSystem.AugmentationType.Atk
                          && !ownedAugments.Contains(aug))
            .ToList();

        // 아직 먹지 않은 수비형 + 유틸형 증강을 따로 뽑음
        List<AugmentationSystem> otherList = augmentationDatabase
            .Where(aug => aug.augmentationType != AugmentationSystem.AugmentationType.Atk
                          && !ownedAugments.Contains(aug))
            .ToList();

        // 1차: 공격형 먼저 최대한 채운다
        while (result.Count < count && attackList.Count > 0)
        {
            int rand = Random.Range(0, attackList.Count);

            // 랜덤으로 하나 뽑아서 결과에 넣음
            result.Add(attackList[rand]);

            // 같은 창에서 중복되지 않도록 리스트에서 제거
            attackList.RemoveAt(rand);
        }

        // 2차: 남은 칸은 수비/유틸로 채운다
        while (result.Count < count && otherList.Count > 0)
        {
            int rand = Random.Range(0, otherList.Count);

            result.Add(otherList[rand]);
            otherList.RemoveAt(rand);
        }

        return result;
    }

    /// <summary>
    /// 버튼에서 증강 하나를 선택했을 때 호출되는 함수
    /// </summary>
    public void SelectAugmentation(AugmentationSystem selectedData)
    {
        // 혹시 비어있는 데이터가 넘어오면 종료
        if (selectedData == null) return;

        // 이미 선택한 적 없는 증강이면 기록
        if (!ownedAugments.Contains(selectedData))
        {
            ownedAugments.Add(selectedData);
        }

        // 여기서 플레이어에게 증강 데이터 전달
        // 중요:
        // PlayerController 쪽에서 ApplyAugmentation(AugmentationSystem aug) 함수가 있어야 함
        // 그리고 그 함수 안에서는 기존 변수만 사용해서 처리하면 됨
        PlayerController.Instance.ApplyAugmentation(selectedData);

        // UI 닫고 게임 재개
        Time.timeScale = 1f;
        uiPanel.SetActive(false);
    }
}