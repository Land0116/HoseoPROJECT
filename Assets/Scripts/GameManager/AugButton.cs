using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AugButton : MonoBehaviour
{
    [Header("버튼 내부 UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    
    [Header("버튼")]
    [SerializeField] private Button selectBtn;
    [SerializeField] private Button rerollBtn;

    // 현재 버튼에 연결된 증강 데이터
    private AugmentationSystem currentData;
    // 이 버튼을 관리하는 UI 매니저
    private AugUIManager currentManager;
    private int slotIndex;

    public void Setup(AugmentationSystem data, AugUIManager manager, int index, bool canReroll)
    {
        currentData = data;
        currentManager = manager;
        slotIndex = index;

        if (nameText != null)
            nameText.text = data != null ? data.augmentationName : "";

        if (descText != null)
            descText.text = data != null ? data.augmentationDesc : "";

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.enabled = data != null && data.icon != null;
        }

        if (selectBtn != null)
        {
            selectBtn.onClick.RemoveAllListeners();
            selectBtn.onClick.AddListener(OnClickSelect);
        }

        if (rerollBtn != null)
        {
            rerollBtn.onClick.RemoveAllListeners();
            rerollBtn.onClick.AddListener(OnClickReroll);

            // 딱 1회만 가능
            rerollBtn.interactable = canReroll;

            // 하이라이트/셀렉트도 끊고 싶으면 Navigation None
            Navigation nav = rerollBtn.navigation;
            nav.mode = Navigation.Mode.None;
            rerollBtn.navigation = nav;
        }
    }

    private void OnClickSelect()
    {
        if (currentManager == null || currentData == null) return;
        currentManager.SelectAugmentation(currentData);
    }

    private void OnClickReroll()
    {
        if (currentManager == null) return;
        if (rerollBtn == null) return;
        if (!rerollBtn.interactable) return;

        currentManager.RerollAugmentation(slotIndex);

        // 혹시 눌린 채 선택 상태 남아있으면 제거
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == rerollBtn.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    
}