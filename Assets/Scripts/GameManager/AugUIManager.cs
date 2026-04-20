using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private const int MaxResetPerSelection = 3;

    #endregion

    #region Fields

    [Header("전체 증강 데이터")]
    [SerializeField] private AugmentationSystem[] augmentationDatabase;

    [Header("현재 표시 중인 3개 카드")]
    [SerializeField] private AugmentationSystem[] currentChoices = new AugmentationSystem[ChoiceCount];

    [Header("UI 버튼 3개")]
    [SerializeField] private AugButton[] uiButtons;

    [Header("증강 선택 패널")]
    [SerializeField] private GameObject uiPanel;

    [Header("리셋 버튼")]
    [SerializeField] private Button resetBtn;
    [SerializeField] private TMP_Text resetCountText;

    [Header("보유 증강 슬롯 UI")]
    [SerializeField] private Image[] slotImages;

    [Header("현재 선택 단계 리셋 잔여 횟수")]
    [SerializeField] private int currentResetCount = MaxResetPerSelection;

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

        if (resetBtn != null)
        {
            resetBtn.onClick.RemoveAllListeners();
            resetBtn.onClick.AddListener(OnClickReset);
        }
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

        currentResetCount = MaxResetPerSelection;
        GenerateNewChoices();
        ApplyCurrentChoicesToButtons();
        RefreshResetUI();
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
            if (ownedSlots[i] == null || !ownedSlots[i].isOccupied || ownedSlots[i].augmentData == null) continue;

            slotImages[i].gameObject.SetActive(true);
            slotImages[i].sprite = ownedSlots[i].augmentData.icon;
            slotImages[i].enabled = ownedSlots[i].augmentData.icon != null;
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
        }

        Transform slotRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugUIPanel");
        if (slotRoot != null)
        {
            Image[] allImages = slotRoot.GetComponentsInChildren<Image>(true);
            List<Image> slotList = new List<Image>();

            for (int i = 0; i < allImages.Length; i++)
            {
                if (allImages[i] == null) continue;
                if (allImages[i].name.Contains("Slot"))
                    slotList.Add(allImages[i]);
            }

            slotImages = slotList.ToArray();
        }

        RefreshOwnedAugmentUI();

        if (resetBtn != null)
        {
            resetBtn.onClick.RemoveAllListeners();
            resetBtn.onClick.AddListener(OnClickReset);
        }
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

    private AugmentationSystem.AugmentCategory AdjustCategoryToAvoidTripleDuplicate(int choiceIndex, AugmentationSystem.AugmentCategory targetCategory)
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

    private AugmentationSystem PickWeightedAugment(List<AugmentationSystem> source, AugmentationSystem.AugmentCategory? categoryFilter)
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

    private void RefreshResetUI()
    {
        if (resetBtn != null)
            resetBtn.interactable = currentResetCount > 0;

        if (resetCountText != null)
            resetCountText.text = $"Reset : {currentResetCount}";
    }

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

    #endregion

    #region Button Event

    private void OnClickReset()
    {
        if (currentResetCount <= 0) return;
        if (uiPanel == null || !uiPanel.activeSelf) return;

        currentResetCount--;
        GenerateNewChoices();
        ApplyCurrentChoicesToButtons();
        RefreshResetUI();
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