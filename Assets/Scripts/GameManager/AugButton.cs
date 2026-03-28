using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugButton : MonoBehaviour
{
    [Header("버튼 내부 UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button selectButton;

    // 현재 버튼에 연결된 증강 데이터
    private AugmentationSystem currentData;

    // 이 버튼을 관리하는 UI 매니저
    private AugUIManager currentManager;

    public void Setup(AugmentationSystem data, AugUIManager manager)
    {
        // 현재 데이터와 매니저 저장
        currentData = data;
        currentManager = manager;

        // 이름 표시
        if (nameText != null)
            nameText.text = data.augmentationName;

        // 설명 표시
        if (descText != null)
            descText.text = data.augmentationDesc;

        // 아이콘 표시
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        // 버튼 클릭 이벤트 연결
        if (selectButton != null)
        {
            // 기존 리스너 제거
            selectButton.onClick.RemoveAllListeners();

            // 현재 증강 선택 함수 연결
            selectButton.onClick.AddListener(OnClickAugmentation);
        }
    }

    public void OnClickAugmentation()
    {
        // 매니저나 데이터가 없으면 종료
        if (currentManager == null || currentData == null) return;

        // UI 매니저에 현재 증강 선택 요청
        currentManager.SelectAugmentation(currentData);
    }
}