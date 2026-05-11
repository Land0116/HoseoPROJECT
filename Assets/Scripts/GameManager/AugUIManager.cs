using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.EventSystems;
using TMPro;

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

    [Header("보유 증강 슬롯 Rect")] [SerializeField]
    private RectTransform[] slotRects;

    [Header("보유 증강 설명창 UI")] [SerializeField]
    private GameObject ownedAugTooltipPanel;

    [SerializeField] private RectTransform ownedAugTooltipRect;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private TMP_Text tooltipCategoryText;
    [SerializeField] private TMP_Text tooltipEffectText;
    [SerializeField] private TMP_Text tooltipDescriptionText;

    [Header("보유 증강 설명창 위치")] 
    [SerializeField] private Vector2 tooltipOffset = new Vector2(24f, 0f);

    [Header("마우스 / 조준점")] 
    [SerializeField] private GameObject crosshairObject;

    private int hoveredOwnedSlotIndex = -1;
    private readonly List<RaycastResult> hoverRaycastResults = new List<RaycastResult>(16);

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
        HideOwnedAugTooltip();
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
        
        RefreshOwnedHoverAfterUpdate();
    }

    public void ResetUIStateForRestart()
    {
        Time.timeScale = 1f;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        HideOwnedAugTooltip();
        SetCursorAndCrosshairForSlotHover(false);

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
        BindOwnedAugTooltipUI(systemUIRoot);

        RefreshOwnedAugmentUI();
        RefreshRerollUI();
    }

    private void BindOwnedSlotImages(GameObject systemUIRoot)
    {
        Transform slotRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugUIPanel");
        if (slotRoot == null)
        {
            slotImages = null;
            slotRects = null;
            Debug.LogWarning("[AugUIManager] AugUIPanel 을 찾지 못함");
            return;
        }

        const int ownedSlotCount = 6;

        Image[] boundImages = new Image[ownedSlotCount];
        RectTransform[] boundRects = new RectTransform[ownedSlotCount];

        for (int i = 0; i < ownedSlotCount; i++)
        {
            int slotNumber = i + 1;

            Transform slotTr = UIManager.FindChildRecursive(slotRoot, $"AugUISlot_{slotNumber}");
            if (slotTr == null)
            {
                Debug.LogWarning($"[AugUIManager] AugUISlot_{slotNumber} 을 찾지 못함");
                continue;
            }

            boundRects[i] = slotTr as RectTransform;

            SetupOwnedSlotPointerEvent(slotTr.gameObject, i);

            Transform bgPanelTr = UIManager.FindChildRecursive(slotTr, $"AugImagePanel_{slotNumber}");
            if (bgPanelTr == null)
            {
                Debug.LogWarning($"[AugUIManager] AugImagePanel_{slotNumber} 을 찾지 못함");
                continue;
            }

            Transform iconTr = UIManager.FindChildRecursive(bgPanelTr, "AugImage");
            if (iconTr == null)
            {
                Debug.LogWarning($"[AugUIManager] 슬롯 {slotNumber} 의 실제 아이콘 오브젝트를 찾지 못함");
                continue;
            }

            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg == null)
            {
                Debug.LogWarning($"[AugUIManager] 슬롯 {slotNumber} 의 아이콘 오브젝트에 Image 컴포넌트가 없음");
                continue;
            }

            boundImages[i] = iconImg;
        }

        slotImages = boundImages;
        slotRects = boundRects;
    }

    #region Owned Augment Tooltip

    private void SetupOwnedSlotPointerEvent(GameObject slotObject, int index)
    {
        if (slotObject == null) return;

        /*
         * 새 스크립트 없이 EventTrigger로 처리한다.
         * 슬롯 데이터는 AugmentRunManager가 관리하고,
         * 마우스 이벤트만 AugUIManager가 받는 구조다.
         */

        Image raycastImage = slotObject.GetComponent<Image>();

        if (raycastImage == null)
        {
            raycastImage = slotObject.AddComponent<Image>();
            raycastImage.color = new Color(1f, 1f, 1f, 0f);
        }

        raycastImage.raycastTarget = true;

        EventTrigger trigger = slotObject.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = slotObject.AddComponent<EventTrigger>();

        /*
         * BindAugUI가 씬 로드 때 다시 호출될 수 있으므로
         * 이벤트가 중복 등록되지 않게 초기화한다.
         */
        trigger.triggers.Clear();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((eventData) =>
        {
            OnHoverOwnedAugmentSlot(index);
        });

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((eventData) =>
        {
            OnExitOwnedAugmentSlot(index);
        });

        trigger.triggers.Add(enterEntry);
        trigger.triggers.Add(exitEntry);
    }

    private void BindOwnedAugTooltipUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        /*
         * 추천 UI 구조
         *
         * System_UI
         * └─ OwnedAugTooltipPanel
         *    ├─ TooltipName
         *    ├─ TooltipCategory
         *    ├─ TooltipEffectText
         *    └─ TooltipDescription
         */

        Transform panelTr = UIManager.FindChildRecursive(systemUIRoot.transform, "AugTooltipDescriptionPanel");
        if (panelTr == null)
            panelTr = UIManager.FindChildRecursive(systemUIRoot.transform, "AugTooltipDescriptionPanel");

        if (panelTr == null)
        {
            Debug.LogWarning("[AugUIManager] AugTooltipDescriptionPanel 을 찾지 못함");
            return;
        }

        ownedAugTooltipPanel = panelTr.gameObject;
        ownedAugTooltipRect = panelTr as RectTransform;

        Transform nameTr = UIManager.FindChildRecursive(panelTr, "TooltipTitle");
        Transform categoryTr = UIManager.FindChildRecursive(panelTr, "TooltipCategory");
        Transform effectTr = UIManager.FindChildRecursive(panelTr, "TooltipEffectText");
        Transform descriptionTr = UIManager.FindChildRecursive(panelTr, "TooltipDescription");

        if (nameTr != null)
            tooltipNameText = nameTr.GetComponent<TMP_Text>();

        if (categoryTr != null)
            tooltipCategoryText = categoryTr.GetComponent<TMP_Text>();

        if (effectTr != null)
            tooltipEffectText = effectTr.GetComponent<TMP_Text>();

        if (descriptionTr != null)
            tooltipDescriptionText = descriptionTr.GetComponent<TMP_Text>();

        DisableTooltipRaycast(ownedAugTooltipPanel.transform);

        ownedAugTooltipPanel.SetActive(false);
    }

    private void DisableTooltipRaycast(Transform root)
    {
        if (root == null) return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    public void OnHoverOwnedAugmentSlot(int index)
    {
        hoveredOwnedSlotIndex = index;

        SetCursorAndCrosshairForSlotHover(true);

        AugmentationSystem data = GetOwnedAugmentBySlotIndex(index);
        if (data == null)
        {
            HideOwnedAugTooltip();
            return;
        }

        int level = GetOwnedAugmentLevelBySlotIndex(index);
        RectTransform slotRect = GetOwnedSlotRect(index);

        ShowOwnedAugTooltip(data, level, slotRect);
    }

    public void OnExitOwnedAugmentSlot(int index)
    {
        if (hoveredOwnedSlotIndex != index)
            return;

        hoveredOwnedSlotIndex = -1;

        HideOwnedAugTooltip();
        SetCursorAndCrosshairForSlotHover(false);
    }

    private AugmentationSystem GetOwnedAugmentBySlotIndex(int index)
    {
        if (AugmentRunManager.Instance == null) return null;

        AugmentSlotData[] ownedSlots = AugmentRunManager.Instance.OwnedSlots;
        if (ownedSlots == null) return null;

        if (index < 0 || index >= ownedSlots.Length) return null;

        AugmentSlotData slotData = ownedSlots[index];
        if (slotData == null) return null;
        if (!slotData.isOccupied) return null;

        return slotData.augmentData;
    }

    private int GetOwnedAugmentLevelBySlotIndex(int index)
    {
        if (AugmentRunManager.Instance == null) return 1;

        AugmentSlotData[] ownedSlots = AugmentRunManager.Instance.OwnedSlots;
        if (ownedSlots == null) return 1;

        if (index < 0 || index >= ownedSlots.Length) return 1;

        AugmentSlotData slotData = ownedSlots[index];
        if (slotData == null) return 1;
        if (!slotData.isOccupied) return 1;
        if (slotData.augmentData == null) return 1;

        AugmentationSystem aug = slotData.augmentData;

        switch (aug.category)
        {
            case AugmentationSystem.AugmentCategory.SubSkill:
                return Mathf.Clamp(slotData.currentLevel, 1, aug.maxLevel);

            case AugmentationSystem.AugmentCategory.Passive:
                return Mathf.Clamp(slotData.stackCount, 1, aug.maxLevel);

            case AugmentationSystem.AugmentCategory.Special:
                return Mathf.Clamp(slotData.currentLevel, 1, aug.maxLevel);
        }

        return 1;
    }

    private RectTransform GetOwnedSlotRect(int index)
    {
        if (slotRects == null) return null;
        if (index < 0 || index >= slotRects.Length) return null;

        return slotRects[index];
    }

    private void ShowOwnedAugTooltip(AugmentationSystem data, int level, RectTransform slotRect)
    {
        if (data == null) return;
        if (ownedAugTooltipPanel == null) return;

        if (tooltipNameText != null)
        {
            tooltipNameText.text = string.IsNullOrWhiteSpace(data.augmentationName)
                ? data.name
                : data.augmentationName;
        }

        if (tooltipCategoryText != null)
        {
            tooltipCategoryText.text = data.GetTooltipCategoryText();
        }

        if (tooltipEffectText != null)
        {
            tooltipEffectText.text = data.GetTooltipEffectText(level);
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.text = data.GetTooltipDescription(level);
        }

        ownedAugTooltipPanel.SetActive(true);

        SetOwnedAugTooltipPosition(slotRect);
    }

    private void HideOwnedAugTooltip()
    {
        if (ownedAugTooltipPanel != null)
            ownedAugTooltipPanel.SetActive(false);
    }

    private void SetOwnedAugTooltipPosition(RectTransform slotRect)
    {
        if (slotRect == null) return;
        if (ownedAugTooltipRect == null) return;

        RectTransform parentRect = ownedAugTooltipRect.parent as RectTransform;
        if (parentRect == null) return;

        Canvas canvas = parentRect.GetComponentInParent<Canvas>();

        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        // corners[2] = 오른쪽 위
        // corners[3] = 오른쪽 아래
        Vector3 rightCenterWorld = (corners[2] + corners[3]) * 0.5f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rightCenterWorld);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            ownedAugTooltipRect.anchoredPosition = localPoint + tooltipOffset;

            LayoutRebuilder.ForceRebuildLayoutImmediate(ownedAugTooltipRect);
            ClampTooltipInsideParent(ownedAugTooltipRect, parentRect);
        }
    }

    private void ClampTooltipInsideParent(RectTransform target, RectTransform parent)
    {
        if (target == null) return;
        if (parent == null) return;

        Vector2 pos = target.anchoredPosition;

        Vector2 targetSize = target.rect.size;
        Vector2 parentSize = parent.rect.size;

        Vector2 targetPivot = target.pivot;
        Vector2 parentPivot = parent.pivot;

        float minX = -parentSize.x * parentPivot.x + targetSize.x * targetPivot.x;
        float maxX = parentSize.x * (1f - parentPivot.x) - targetSize.x * (1f - targetPivot.x);

        float minY = -parentSize.y * parentPivot.y + targetSize.y * targetPivot.y;
        float maxY = parentSize.y * (1f - parentPivot.y) - targetSize.y * (1f - targetPivot.y);

        if (minX <= maxX)
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY <= maxY)
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        target.anchoredPosition = pos;
    }

    private void SetCursorAndCrosshairForSlotHover(bool isHover)
    {
        Cursor.visible = isHover;

        GameObject crosshair = GetCrosshairObject();

        if (crosshair != null)
            crosshair.SetActive(!isHover);
    }

    private GameObject GetCrosshairObject()
    {
        if (crosshairObject != null)
            return crosshairObject;

        GameObject found = GameObject.Find("Crosshair");

        if (found == null)
            found = GameObject.Find("CrossHair");

        if (found != null)
            crosshairObject = found;

        return crosshairObject;
    }

    private void RefreshOwnedHoverAfterUpdate()
    {
        if (EventSystem.current == null) return;
        if (Mouse.current == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        hoverRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, hoverRaycastResults);

        for (int i = 0; i < hoverRaycastResults.Count; i++)
        {
            int slotIndex = FindOwnedSlotIndexFromRaycastObject(hoverRaycastResults[i].gameObject);

            if (slotIndex >= 0)
            {
                OnHoverOwnedAugmentSlot(slotIndex);
                return;
            }
        }

        if (hoveredOwnedSlotIndex >= 0)
        {
            hoveredOwnedSlotIndex = -1;
            HideOwnedAugTooltip();
            SetCursorAndCrosshairForSlotHover(false);
        }
    }
    private int FindOwnedSlotIndexFromRaycastObject(GameObject hitObject)
    {
        if (hitObject == null) return -1;
        if (slotRects == null) return -1;

        Transform current = hitObject.transform;

        while (current != null)
        {
            for (int i = 0; i < slotRects.Length; i++)
            {
                if (slotRects[i] == null) continue;

                if (current == slotRects[i])
                    return i;
            }

            current = current.parent;
        }

        return -1;
    }

    #endregion

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

        return true;
    }

    #endregion
}