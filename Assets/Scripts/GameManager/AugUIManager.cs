using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using UnityEngine.EventSystems;
using TMPro;

public class AugUIManager : MonoBehaviour
{
    public static AugUIManager instance;
    private Action onRewardAugmentFinished;

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

    [Header("리롤 버튼 패널")] [SerializeField] private GameObject rerollButtonPanel;

    [Header("보유 증강 슬롯 UI")] [SerializeField]
    private Image[] slotImages;

    [Header("보유 증강 슬롯 배경 이미지")] [SerializeField]
    private Image[] slotFrameImages;

    [Header("슬롯칸 스프라이트")] [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite occupiedSlotSprite;

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

    private const float TooltipOffsetDefaultX = 6f;
    private const float TooltipOffsetDefaultY = 230f;

    [Header("보유 증강 설명창 위치")] [SerializeField]
    private bool useCodeTooltipOffsetDefault = true;

    [SerializeField] private Vector2 tooltipOffset =
        new Vector2(TooltipOffsetDefaultX, TooltipOffsetDefaultY);

    private void OnValidate()
    {
        if (!useCodeTooltipOffsetDefault)
            return;

        tooltipOffset = new Vector2(
            TooltipOffsetDefaultX,
            TooltipOffsetDefaultY
        );
    }

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

            SetRerollPanelVisible(false);
            return;
        }

        if (!CanShowAugmentation())
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);

            SetRerollPanelVisible(false);
            return;
        }

        Time.timeScale = 0f;
        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.AugmentPanel, true);

        if (uiPanel == null) return;

        uiPanel.SetActive(true);
        SetRerollPanelVisible(true);

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

            // 아이콘 변경
            slotImages[i].sprite = aug.slotSprite;
            slotImages[i].enabled = aug.slotSprite != null;
            slotImages[i].color = Color.white;

            // 슬롯칸 스프라이트 변경
            if (slotFrameImages != null &&
                i < slotFrameImages.Length &&
                slotFrameImages[i] != null &&
                occupiedSlotSprite != null)
            {
                slotFrameImages[i].sprite = occupiedSlotSprite;
                slotFrameImages[i].enabled = true;
                slotFrameImages[i].color = Color.white;
            }
        }

        RefreshOwnedHoverAfterUpdate();
    }

    public void ResetUIStateForRestart()
    {
        Time.timeScale = 1f;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        SetRerollPanelVisible(false);

        HideOwnedAugTooltip();
        SetCursorAndCrosshairForSlotHover(false);

        ClearOwnedAugmentUI();
        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.AugmentPanel, false);
    }

    public bool IsAugmentationVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    public void HideCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);

        SetRerollPanelVisible(false);
    }

    public void RestoreCurrentAugmentationUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(true);

        SetRerollPanelVisible(true);
        RefreshRerollUI();

        Time.timeScale = 0f;

        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.AugmentPanel, true);
    }

    public void BindAugUI(GameObject systemUIRoot)
    {
        if (systemUIRoot == null) return;

        Transform augPanelRoot = UIManager.FindChildRecursive(systemUIRoot.transform, "AugmentationUIPanel");
        if (augPanelRoot != null)
        {
            uiPanel = augPanelRoot.gameObject;
            uiButtons = augPanelRoot.GetComponentsInChildren<AugButton>(true);

            BindRerollButtons(systemUIRoot, augPanelRoot);
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
            slotFrameImages = null;
            slotRects = null;
            Debug.LogWarning("[AugUIManager] AugUIPanel 을 찾지 못함");
            return;
        }

        const int ownedSlotCount = 6;

        Image[] boundImages = new Image[ownedSlotCount];
        Image[] boundFrameImages = new Image[ownedSlotCount];
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

            SetupOwnedSlotPointerEvent(slotTr.gameObject, i);

            Transform bgPanelTr = UIManager.FindChildRecursive(slotTr, $"AugImagePanel_{slotNumber}");
            if (bgPanelTr == null)
            {
                Debug.LogWarning($"[AugUIManager] AugImagePanel_{slotNumber} 을 찾지 못함");
                continue;
            }

            RectTransform bgRect = bgPanelTr as RectTransform;
            if (bgRect == null)
            {
                Debug.LogWarning($"[AugUIManager] AugImagePanel_{slotNumber} 에 RectTransform이 없음");
                continue;
            }

            Image bgImage = bgPanelTr.GetComponent<Image>();
            if (bgImage == null)
            {
                Debug.LogWarning($"[AugUIManager] AugImagePanel_{slotNumber} 에 Image 컴포넌트가 없음");
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

            // 아이콘 이미지
            boundImages[i] = iconImg;

            // 슬롯칸 배경 이미지
            boundFrameImages[i] = bgImage;

            // 설명창 위치 기준
            boundRects[i] = bgRect;
        }

        slotImages = boundImages;
        slotFrameImages = boundFrameImages;
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
        enterEntry.callback.AddListener((eventData) => { OnHoverOwnedAugmentSlot(index); });

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((eventData) => { OnExitOwnedAugmentSlot(index); });

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

    private bool IsOwnedAugmentSlotUIVisible()
    {
        if (slotImages == null || slotImages.Length == 0)
            return false;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            if (slotImages[i].gameObject.activeInHierarchy && slotImages[i].enabled)
                return true;
        }

        return false;
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

        /*
         * 핵심:
         * 설명창의 기준점을 왼쪽 위로 잡는다.
         *
         * pivot (0, 1)
         * X = 왼쪽 기준
         * Y = 위쪽 기준
         *
         * 이렇게 해야 설명창이 위로 커지는 게 아니라 아래로 펼쳐진다.
         */
        ownedAugTooltipRect.pivot = new Vector2(0f, 1f);

        /*
         * 부모 좌표계 기준으로 직접 배치할 것이므로
         * Anchor는 중앙 고정으로 통일한다.
         */
        ownedAugTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        ownedAugTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);

        /*
         * 텍스트와 Layout 크기 먼저 갱신.
         * pivot이 왼쪽 위라서 높이가 변해도 아래로 늘어난다.
         */
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(ownedAugTooltipRect);

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        /*
         * corners[2] = 슬롯 오른쪽 위
         *
         * 원하는 위치:
         * 슬롯 오른쪽 위에 설명창 왼쪽 위를 맞춘다.
         */
        Vector3 slotRightTopWorld = corners[2];

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            slotRightTopWorld
        );

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            ownedAugTooltipRect.anchoredPosition = localPoint + tooltipOffset;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(ownedAugTooltipRect);
        }
    }

    private void SetCursorAndCrosshairForSlotHover(bool isHovering)
    {
        if (isHovering)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            CrosshairUI.Instance?.HideCrosshair();
            return;
        }

        bool canShowCrosshair =
            SceneManager.GetActiveScene().name != "Main" &&
            PlayerController.Instance != null &&
            !PlayerController.Instance.IsDie &&
            !PlayerController.Instance.IsSystemInputLocked();

        if (canShowCrosshair)
        {
            CrosshairUI.Instance?.ShowCrosshair();
        }
        else
        {
            CrosshairUI.Instance?.HideCrosshair();
        }
    }


    private void RefreshOwnedHoverAfterUpdate()
    {
        if (!IsOwnedAugmentSlotUIVisible())
        {
            if (hoveredOwnedSlotIndex >= 0)
            {
                hoveredOwnedSlotIndex = -1;
                HideOwnedAugTooltip();
            }

            return;
        }

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

            // 핵심:
            // 같은 참조 1개만 제거하지 말고,
            // 같은 augmentID를 가진 후보를 전부 제거한다.
            RemoveSameAugmentFromCandidateBuffer(picked);
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

    private void RemoveSameAugmentFromCandidateBuffer(AugmentationSystem picked)
    {
        if (picked == null)
            return;

        for (int i = candidateBuffer.Count - 1; i >= 0; i--)
        {
            AugmentationSystem candidate = candidateBuffer[i];

            if (IsSameAugmentChoice(candidate, picked))
            {
                candidateBuffer.RemoveAt(i);
            }
        }
    }

    private void RemoveCurrentChoicesFromCandidateBuffer()
    {
        for (int i = candidateBuffer.Count - 1; i >= 0; i--)
        {
            AugmentationSystem candidate = candidateBuffer[i];

            if (IsSameAsAnyCurrentChoice(candidate))
            {
                candidateBuffer.RemoveAt(i);
            }
        }
    }

    private bool IsSameAsAnyCurrentChoice(AugmentationSystem candidate)
    {
        if (candidate == null) return false;
        if (currentChoices == null) return false;

        for (int i = 0; i < currentChoices.Length; i++)
        {
            AugmentationSystem current = currentChoices[i];

            if (current == null)
                continue;

            if (IsSameAugmentChoice(candidate, current))
                return true;
        }

        return false;
    }

    private bool IsSameAugmentChoice(AugmentationSystem a, AugmentationSystem b)
    {
        if (a == null || b == null)
            return false;

        // 가장 우선 기준: augmentID
        // 같은 증강이면 반드시 같은 ID를 쓰는 구조가 가장 안전함.
        if (!string.IsNullOrEmpty(a.augmentID) &&
            !string.IsNullOrEmpty(b.augmentID))
        {
            return a.augmentID == b.augmentID;
        }

        // augmentID가 비어 있는 예외 상황 대비
        // 같은 ScriptableObject 참조면 같은 증강으로 본다.
        return a == b;
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
        if (slotImages != null)
        {
            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] == null) continue;

                slotImages[i].sprite = null;
                slotImages[i].enabled = false;
            }
        }

        if (slotFrameImages != null)
        {
            for (int i = 0; i < slotFrameImages.Length; i++)
            {
                if (slotFrameImages[i] == null) continue;

                if (emptySlotSprite != null)
                {
                    slotFrameImages[i].sprite = emptySlotSprite;
                }

                slotFrameImages[i].enabled = true;
                slotFrameImages[i].color = Color.white;
            }
        }
    }

    private void SetRerollPanelVisible(bool visible)
    {
        if (rerollButtonPanel != null)
        {
            rerollButtonPanel.SetActive(visible);
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

    private void BindRerollButtons(GameObject systemUIRoot, Transform augPanelRoot)
    {
        if (resetBtn == null || resetBtn.Length != ChoiceCount)
            resetBtn = new Button[ChoiceCount];

        for (int i = 0; i < resetBtn.Length; i++)
        {
            resetBtn[i] = null;
        }

        Transform rerollRoot = null;

        // 1순위: AugmentationUIPanel 안의 RerollButtonPanel
        if (augPanelRoot != null)
        {
            rerollRoot = UIManager.FindChildRecursive(
                augPanelRoot,
                "AugmentationUIResetPanel"
            );
        }

        // 2순위: System_UI 전체에서 RerollButtonPanel 찾기
        // 혹시 RerollButtonPanel을 AugmentationUIPanel 밖에 뒀을 때 대비
        if (rerollRoot == null && systemUIRoot != null)
        {
            rerollRoot = UIManager.FindChildRecursive(
                systemUIRoot.transform,
                "AugmentationUIResetPanel"
            );
        }

        if (rerollRoot == null)
        {
            rerollButtonPanel = null;
            Debug.LogWarning("[AugUIManager] AugmentationUIResetPanel 을 찾지 못함");
            return;
        }

        rerollButtonPanel = rerollRoot.gameObject;

        for (int i = 0; i < ChoiceCount; i++)
        {
            Transform rerollBtnTr = UIManager.FindChildRecursive(
                rerollRoot,
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

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickReroll(capturedIndex));

            btn.interactable = false;

            // 키보드/패드 네비게이션 때문에 이상한 선택 하이라이트가 생기는 것 방지
            Navigation navigation = btn.navigation;
            navigation.mode = Navigation.Mode.None;
            btn.navigation = navigation;
        }

        RefreshRerollUI();
    }

    private void RefreshRerollUI()
    {
        bool panelVisible = uiPanel != null && uiPanel.activeSelf;

        SetRerollPanelVisible(panelVisible);

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

            resetBtn[i].interactable =
                panelVisible &&
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

        AugmentationSystem rerolled = GenerateSingleChoiceForRerollSlot(choiceIndex);
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

    private AugmentationSystem GenerateSingleChoiceForRerollSlot(int rerollSlotIndex)
    {
        candidateBuffer.Clear();
        FillCandidateBuffer();

        // 핵심:
        // 현재 화면에 떠 있는 다른 선택지들과 겹치는 후보 제거.
        // rerollSlotIndex까지 포함해서 제거하면
        // 리롤했는데 똑같은 카드가 다시 나오는 것도 막을 수 있다.
        RemoveCurrentChoicesFromCandidateBuffer();

        if (candidateBuffer.Count == 0)
            return null;

        AugmentationSystem.AugmentCategory targetCategory = GetWeightedCategory();

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

        SetRerollPanelVisible(false);

        Time.timeScale = 1f;

        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.AugmentPanel, false);

        NotifyRewardAugmentFinished();
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

    #region Action

    public void OpenAugmentPanelFromReward(Action onFinished)
    {
        onRewardAugmentFinished = onFinished;

        ShowAugmentation();

        // ShowAugmentation()이 실패해서 패널이 열리지 않았다면
        // 전투방이 막히지 않도록 보상 완료 처리
        if (!IsAugmentationVisible())
        {
            Debug.LogWarning("[AugUIManager] 증강 패널을 열 수 없어 보상 완료 처리");
            NotifyRewardAugmentFinished();
        }
    }

    private void NotifyRewardAugmentFinished()
    {
        onRewardAugmentFinished?.Invoke();
        onRewardAugmentFinished = null;
    }

    #endregion
}