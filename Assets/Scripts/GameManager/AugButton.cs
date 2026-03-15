using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugButton : MonoBehaviour
{
    [Header("UI 텍스트")]
    // 증강 이름을 표시할 텍스트
    [SerializeField] private TMP_Text nameText;

    // 증강 설명을 표시할 텍스트
    [SerializeField] private TMP_Text descriptionText;

    [Header("버튼 컴포넌트")]
    // 실제 Unity UI Button 컴포넌트
    [SerializeField] private Button button;

    // 이 버튼이 현재 들고 있는 증강 데이터
    // 버튼마다 각각 다른 증강이 들어감
    private AugmentationSystem currentAugment;

    // 어떤 매니저에게 선택 사실을 알려줄지 저장
    private AugUIManager uiManager;

    /// <summary>
    /// 버튼에 증강 데이터를 세팅하는 함수
    /// AugUIManager가 ShowAugmentation()에서 호출함
    /// </summary>
    public void Setup(AugmentationSystem augmentData, AugUIManager manager)
    {
        // 현재 버튼에 들어갈 증강 데이터 저장
        currentAugment = augmentData;

        // 클릭 시 알려줄 매니저 저장
        uiManager = manager;

        // UI에 이름 / 설명 표시
        nameText.text = augmentData.augmentationName;
        descriptionText.text = augmentData.augmentationDesc;

        // 버튼 클릭 이벤트 중복 등록 방지
        button.onClick.RemoveAllListeners();

        // 버튼 클릭 시 OnClickButton 함수 실행
        button.onClick.AddListener(OnClickButton);
    }

    /// <summary>
    /// 버튼을 눌렀을 때 실행되는 함수
    /// 현재 버튼이 들고 있는 증강 데이터를 매니저에게 넘긴다
    /// </summary>
    private void OnClickButton()
    {
        // 혹시 데이터나 매니저가 비어있으면 종료
        if (currentAugment == null || uiManager == null) return;

        // 선택된 증강 데이터를 매니저에 전달
        uiManager.SelectAugmentation(currentAugment);
    }
}