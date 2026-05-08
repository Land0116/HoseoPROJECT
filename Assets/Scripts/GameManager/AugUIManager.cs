using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AugUIManager : MonoBehaviour
{
    public static AugUIManager instance;

    #region Constants

    private const int ChoiceCount = 3;

    #endregion

    #region Fields

    [Header("전체 증강 데이터")] [SerializeField] private AugmentationSystem[] augmentationDatabase;

    [Header("현재 표시 중인 3개 카드")] [SerializeField]
    private AugmentationSystem[] currentChoices = new AugmentationSystem[ChoiceCount];

    [Header("UI 버튼 3개")] [SerializeField] private AugButton[] uiButtons;

    [Header("증강 선택 패널")] [SerializeField] private GameObject uiPanel;

    [Header("리셋 버튼")] [SerializeField] private Button[] resetBtn;
    private bool[] rerollUsed = new bool[ChoiceCount];

    [Header("보유 증강 슬롯 UI")] [SerializeField]
    private Image[] slotImages;

    private readonly List<AugmentationSystem> candidateBuffer = new List<AugmentationSystem>(64);

    #endregion

    #region Unity

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        ClearOwnedAugmentUI();
        //RefreshRerollUI();
    }

    #endregion

    #region Public

    public bool CanShowAugmentation()
    {
        if (AugmentRunManager.Instance == null) return false;
        if (augmentationDatabase == null || augmentationDatabase.Length == 0) return false;

        for (int i = 0; i < augmentationDatabase.Length; i++)
        {
            AugmentationSystem aug = augmentationDatabase[i];
            if (aug == null) continue;
            if (!aug.isUnlocked) continue;

            if (AugmentRunManager.Instance.CanOfferAugment(aug))
                return true;
        }

        return false;
    }

    public void ShowAugmentation()
    {
        if (!CanOpenAugUIInCurrentScene())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);
            return;
        }

        if (!CanShowAugmentation())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);
            return;
        }

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetPause(true);
            PlayerController.Instance.SetControl(false);
        }

        if (uiPanel == null) return;
        uiPanel.SetActive(true);

        ResetRerollState();
        GenerateNewChoices();
        ApplyCurrentChoicesToButtons();
        RefreshRerollUI();
    }

    public void SelectAugmentation(AugmentationSystem selectedData)
    {
        if (selectedData == null) return;
        if (AugmentRunManager.Instance == null) return;

        bool added = AugmentRunManager.Instance.TryAddAugment(selectedData);
        if (!added) return;

        RefreshOwnedAugmentUI();

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowPlayerHUD();
        }

        StartCoroutine(CloseAugmentationAfterMouseRelease());
    }

    public void RefreshOwnedAugmentUI()
    {
        ClearOwnedAugmentUI();

        if (AugmentRunManager.Instance == null) return;
        if (slotImages == null) return;

        AugmentSlotData[] ownedSlots = AugmentRunManager.Instance.OwnedSlots;
        int slotCount = Mathf.Min(AugmentRunManager.Instance.CurrentSlotCount, slotImages.Length);

        for (int i = 0; i < slotCount; i++)
        {
            if (slotImages[i] == null) continue;
            if (ownedSlots[i] == null) continue;
            if (!ownedSlots[i].isOccupied) continue;
            if (ownedSlots[i].augmentData == null) continue;

            AugmentationSystem aug = ownedSlots[i].augmentData;

            // 핵심:
            // 보유 슬롯에는 카드 아이콘(icon)이 아니라 슬롯 전용 이미지(slotSprite)를 넣는다.
            slotImages[i].sprite = aug.slotSprite;
            slotImages[i].enabled = aug.slotSprite != null;

            // 카테고리별 색상 적용 없음
            slotImages[i].color = Color.white;
        }
    }

    public void ResetUIStateForRestart()
    {
        Time.timeScale = 1f;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        ClearOwnedAugmentUI();

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(false);
    }

    public bool IsAugmentationVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    public void HideCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    public void RestoreCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(true);

        Time.timeScale = 0f;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetPause(true);
    }

    public void BindAugUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform augPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugmentationUIPanel");
        if (augPanelRoot != null)
        {
            uiPanel = augPanelRoot.gameObject;
            uiButtons = augPanelRoot.GetComponentsInChildren<AugButton>(true);

            BindresetBtn(augPanelRoot);
        }

        BindOwnedSlotImages(systemUIRoot);

        RefreshOwnedAugmentUI();
        RefreshRerollUI();
    }

    private void BindOwnedSlotImages(GameObject systemUIRoot)
    {
        Transform slotRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugUIPanel");
        if (slotRoot == null)
        {
            slotImages = null;
            Debug.LogWarning("[AugUIManager] AugUIPanel 을 찾지 못함");
            return;
        }

        List<Image> iconList = new List<Image>();

        // 네 슬롯 수가 6개니까 6으로 고정
        for (int i = 1; i <= 6; i++)
        {
            Transform slotTr = UIManager.FindChildRecursive(slotRoot, $"AugUISlot_{i}");
            if (slotTr == null)
            {
                Debug.LogWarning($"[AugUIManager] AugUISlot_{i} 을 찾지 못함");
                continue;
            }

            Transform bgPanelTr = UIManager.FindChildRecursive(slotTr, $"AugImagePanel_{i}");
            if (bgPanelTr == null)
            {
                Debug.LogWarning($"[AugUIManager] AugImagePanel_{i} 을 찾지 못함");
                continue;
            }

            // 실제 아이콘 이름: AugIamge (현재 네 Hierarchy 기준)
            Transform iconTr = UIManager.FindChildRecursive(bgPanelTr, "AugImage");
            if (iconTr == null)
            {
                Debug.LogWarning($"[AugUIManager] 슬롯 {i} 의 실제 아이콘 오브젝트를 찾지 못함");
                continue;
            }

            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg == null)
            {
                Debug.LogWarning($"[AugUIManager] 슬롯 {i} 의 아이콘 오브젝트에 Image 컴포넌트가 없음");
                continue;
            }

            iconList.Add(iconImg);
        }

        slotImages = iconList.ToArray();

        //Debug.Log($"[AugUIManager] 보유 증강 아이콘 바인딩 완료: {slotImages.Length}개");
    }

    #endregion

    #region Choice Generate

    private void GenerateNewChoices()
    {
        for (int i = 0; i < currentChoices.Length; i++)
        {
            currentChoices[i] = null;
        }

        candidateBuffer.Clear();
        FillCandidateBuffer();

        for (int i = 0; i < ChoiceCount; i++)
        {
            if (candidateBuffer.Count == 0)
                break;

            AugmentationSystem.AugmentCategory targetCategory = GetWeightedCategory();
            targetCategory = AdjustCategoryToAvoidTripleDuplicate(i, targetCategory);

            AugmentationSystem picked = PickWeightedAugment(candidateBuffer, targetCategory);

            if (picked == null)
            {
                picked = PickWeightedAugment(candidateBuffer, null);
            }

            if (picked == null)
                break;

            currentChoices[i] = picked;
            candidateBuffer.Remove(picked);
        }
    }

    private void FillCandidateBuffer()
    {
        if (augmentationDatabase == null || AugmentRunManager.Instance == null)
            return;

        for (int i = 0; i < augmentationDatabase.Length; i++)
        {
            AugmentationSystem aug = augmentationDatabase[i];
            if (aug == null) continue;
            if (!aug.isUnlocked) continue;
            if (!AugmentRunManager.Instance.CanOfferAugment(aug)) continue;

            candidateBuffer.Add(aug);
        }
    }

    private AugmentationSystem.AugmentCategory GetWeightedCategory()
    {
        float rand = Random.Range(0f, 100f);

        if (rand < 40f) return AugmentationSystem.AugmentCategory.SubSkill;
        if (rand < 80f) return AugmentationSystem.AugmentCategory.Passive;
        return AugmentationSystem.AugmentCategory.Special;
    }

    private AugmentationSystem.AugmentCategory AdjustCategoryToAvoidTripleDuplicate(int choiceIndex,
        AugmentationSystem.AugmentCategory targetCategory)
    {
        if (choiceIndex < 2) return targetCategory;
        if (currentChoices[0] == null || currentChoices[1] == null) return targetCategory;

        if (currentChoices[0].category != currentChoices[1].category)
            return targetCategory;

        if (currentChoices[0].category != targetCategory)
            return targetCategory;

        AugmentationSystem.AugmentCategory blockedCategory = currentChoices[0].category;

        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            if (candidateBuffer[i] == null) continue;
            if (candidateBuffer[i].category != blockedCategory)
                return candidateBuffer[i].category;
        }

        return targetCategory;
    }

    private AugmentationSystem PickWeightedAugment(List<AugmentationSystem> source,
        AugmentationSystem.AugmentCategory? categoryFilter)
    {
        int totalWeight = 0;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] == null) continue;
            if (categoryFilter.HasValue && source[i].category != categoryFilter.Value) continue;

            totalWeight += Mathf.Max(1, source[i].weight);
        }

        if (totalWeight <= 0)
            return null;

        int randomValue = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < source.Count; i++)
        {
            AugmentationSystem aug = source[i];
            if (aug == null) continue;
            if (categoryFilter.HasValue && aug.category != categoryFilter.Value) continue;

            cumulative += Mathf.Max(1, aug.weight);

            if (randomValue < cumulative)
                return aug;
        }

        return null;
    }

    #endregion

    #region UI Refresh

    private void ApplyCurrentChoicesToButtons()
    {
        if (uiButtons == null) return;

        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (uiButtons[i] == null) continue;

            if (i < currentChoices.Length && currentChoices[i] != null)
            {
                uiButtons[i].gameObject.SetActive(true);
                int previewLevel = 1;

                if (AugmentRunManager.Instance != null && currentChoices[i] != null)
                {
                    previewLevel = AugmentRunManager.Instance.GetPreviewLevel(currentChoices[i]);
                }

                uiButtons[i].Setup(currentChoices[i], this, previewLevel);
            }
            else
            {
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }


    private void ClearOwnedAugmentUI()
    {
        if (slotImages == null) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            slotImages[i].sprite = null;
            slotImages[i].enabled = false;
        }
    }

    #endregion

    #region Button Event

    private void ResetRerollState()
    {
        if (rerollUsed == null || rerollUsed.Length != ChoiceCount)
            rerollUsed = new bool[ChoiceCount];

        for (int i = 0; i < rerollUsed.Length; i++)
        {
            rerollUsed[i] = false;
        }
    }

    private void BindresetBtn(Transform augPanelRoot)
    {
        if (augPanelRoot == null) return;

        if (resetBtn == null || resetBtn.Length != ChoiceCount)
            resetBtn = new Button[ChoiceCount];

        for (int i = 0; i < ChoiceCount; i++)
        {
            resetBtn[i] = null;

            Transform rerollBtnTr = UIManager.FindChildRecursive(
                augPanelRoot,
                $"RerollBtn_{i + 1}"
            );

            if (rerollBtnTr == null)
            {
                Debug.LogWarning($"[AugUIManager] RerollBtn_{i + 1} 을 찾지 못함");
                continue;
            }

            Button btn = rerollBtnTr.GetComponent<Button>();

            if (btn == null)
            {
                Debug.LogWarning($"[AugUIManager] RerollBtn_{i + 1} 에 Button 컴포넌트가 없음");
                continue;
            }

            resetBtn[i] = btn;

            int capturedIndex = i;
            resetBtn[i].onClick.RemoveAllListeners();
            resetBtn[i].onClick.AddListener(() => OnClickReroll(capturedIndex));

            // 기본값은 비활성화.
            // 증강 패널이 열리고 선택지가 생성된 뒤 RefreshRerollUI()에서 켜준다.
            resetBtn[i].interactable = false;
        }
    }

    private void RefreshRerollUI()
    {
        if (resetBtn == null) return;

        for (int i = 0; i < resetBtn.Length; i++)
        {
            if (resetBtn[i] == null) continue;

            bool hasChoice =
                currentChoices != null &&
                i < currentChoices.Length &&
                currentChoices[i] != null;

            bool used =
                rerollUsed != null &&
                i < rerollUsed.Length &&
                rerollUsed[i];

            // 패널이 켜져 있고, 선택지가 있고, 아직 리롤을 안 쓴 경우만 클릭 가능
            resetBtn[i].interactable =
                uiPanel != null &&
                uiPanel.activeSelf &&
                hasChoice &&
                !used;
        }
    }

    private void OnClickReroll(int choiceIndex)
    {
        Debug.Log($"[AugUIManager] 리롤 버튼 클릭됨 / index: {choiceIndex}");

        if (choiceIndex < 0 || choiceIndex >= ChoiceCount) return;
        if (uiPanel == null || !uiPanel.activeSelf) return;
        if (rerollUsed == null || choiceIndex >= rerollUsed.Length) return;
        if (rerollUsed[choiceIndex]) return;

        AugmentationSystem rerolled = GenerateSingleChoiceForRerollSlot();
        if (rerolled == null)
        {
            Debug.LogWarning("[AugUIManager] 리롤 후보가 없음");
            return;
        }

        currentChoices[choiceIndex] = rerolled;
        rerollUsed[choiceIndex] = true;

        ApplyChoiceToButton(choiceIndex);
        RefreshRerollUI();
    }

    private AugmentationSystem GenerateSingleChoiceForRerollSlot()
    {
        candidateBuffer.Clear();
        FillCandidateBuffer();

        if (candidateBuffer.Count == 0)
            return null;

        // 기존과 동일한 카테고리 확률 규칙
        AugmentationSystem.AugmentCategory targetCategory = GetWeightedCategory();

        // 기존과 동일한 weight 규칙
        AugmentationSystem picked = PickWeightedAugment(candidateBuffer, targetCategory);

        if (picked == null)
            picked = PickWeightedAugment(candidateBuffer, null);

        return picked;
    }

    private void ApplyChoiceToButton(int index)
    {
        if (uiButtons == null) return;
        if (index < 0 || index >= uiButtons.Length) return;
        if (uiButtons[index] == null) return;

        if (index < currentChoices.Length && currentChoices[index] != null)
        {
            uiButtons[index].gameObject.SetActive(true);

            int previewLevel = 1;

            if (AugmentRunManager.Instance != null)
            {
                previewLevel = AugmentRunManager.Instance.GetPreviewLevel(currentChoices[index]);
            }

            uiButtons[index].Setup(currentChoices[index], this, previewLevel);
        }
        else
        {
            uiButtons[index].gameObject.SetActive(false);
        }
    }

    #endregion

    #region Coroutine

    private IEnumerator CloseAugmentationAfterMouseRelease()
    {
        while (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            yield return null;
        }

        yield return null;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        Time.timeScale = 1f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetPause(false);
            PlayerController.Instance.SetControl(true);
        }
    }

    #endregion

    #region Scene Rule

    private bool CanOpenAugUIInCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Main") return false;
        if (sceneName == "Stage_1_5") return false;

        return true;
    }

    #endregion
}