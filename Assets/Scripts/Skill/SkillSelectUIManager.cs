using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectUIManager : MonoBehaviour
{
    public static SkillSelectUIManager Instance;

    [Header("데이터")]
    public SkillData[] skillDatabase;

    [Header("UI")]
    public GameObject panel;
    public SkillSelectButton[] buttons;
    [SerializeField] private Button closeSkillButton;

    private SkillData selectedSkill;
    private bool isSkillOpened = false;

    private GameObject overlayUI;

    private bool isShopMode = false;

    private enum SelectState
    {
        None,
        SkillSelected
    }

    private SelectState state;

    public bool IsSkillOpened
    {
        get => isSkillOpened;
        set
        {
            if (isSkillOpened == value) return;

            isSkillOpened = value;

            if (isSkillOpened)
                OpenPanel();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        isSkillOpened = false;
        panel = null;
    }

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        isSkillOpened = false;
    }

    public void BindSkillUI(GameObject systemUIRoot, GameObject overlay)
    {

        overlayUI = overlay;

        if (overlayUI != null)
            overlayUI.SetActive(false);

        Transform root = UIManager.FindChildRecursive(systemUIRoot.transform, "SkillSelectPanel");

        if (root != null)
        {
            panel = root.gameObject;

            buttons = root.GetComponentsInChildren<SkillSelectButton>(true);

            panel.SetActive(false);
            isSkillOpened = false;

            Transform closeBtn = UIManager.FindChildRecursive(root, "CloseSkillPanelButton");

            if (closeBtn != null)
            {
                closeSkillButton = closeBtn.GetComponent<Button>();
                closeSkillButton.onClick.RemoveAllListeners();
                closeSkillButton.onClick.AddListener(CancelSelection);
            }
        }

        

    }

    private void OpenPanel()
    {
        if (isShopMode) return;

        if (panel == null)
        {

            return;
        }

        panel.SetActive(true);
        Time.timeScale = 0f;

        if (closeSkillButton != null)
            closeSkillButton.gameObject.SetActive(true);

        List<SkillData> randomSkills = skillDatabase
            .OrderBy(x => Random.value)
            .Take(2)
            .ToList();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < randomSkills.Count)
            {
                buttons[i].Setup(randomSkills[i], this);
                buttons[i].gameObject.SetActive(true);
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        selectedSkill = null;
        state = SelectState.None;
    }

    public void SelectSkill(SkillData skill)
    {
        selectedSkill = skill;
        state = SelectState.SkillSelected;

        bool isNewSkill =
            SkillManager.Instance.qSkill != skill &&
            SkillManager.Instance.eSkill != skill;

        if (overlayUI != null)
            overlayUI.SetActive(isNewSkill);

        if (!isNewSkill)
        {
            if (SkillManager.Instance.qSkill == skill)
            {
                SkillManager.Instance.EquipSkill(skill, SkillSlotType.Q);
                ClosePanel();
                return;
            }

            if (SkillManager.Instance.eSkill == skill)
            {
                SkillManager.Instance.EquipSkill(skill, SkillSlotType.E);
                ClosePanel();
                return;
            }
        }
    }

    public void AssignToSlot(SkillSlotType slot)
    {
        if (state != SelectState.SkillSelected)
            return;

        SkillManager.Instance.EquipSkill(selectedSkill, slot);

        state = SelectState.None;
        selectedSkill = null;

        if (overlayUI != null)
            overlayUI.SetActive(false);

        isShopMode = false;

        ClosePanel();
    }
    public void ForceSelectSkillFromShop(SkillData skill)
    {
        selectedSkill = skill;
        state = SelectState.SkillSelected;

        isShopMode = true;

        // panel안열기
        if (panel != null)
            panel.SetActive(false);

        // overlay만 켜서 Q/E 클릭 유도
        if (overlayUI != null)
            overlayUI.SetActive(true);

        Time.timeScale = 0f;
    }



    public void ReplaceSkill(SkillSlotType slot)
    {
        if (state != SelectState.SkillSelected) return;

        SkillManager.Instance.EquipSkill(selectedSkill, slot);

        state = SelectState.None;
        selectedSkill = null;
        isShopMode = false;

        if (overlayUI != null)
            overlayUI.SetActive(false);

        ClosePanel();
    }

    private void CancelSelection()
    {
        selectedSkill = null;

        if (overlayUI != null)
            overlayUI.SetActive(false);

        isShopMode = false;

        ClosePanel();
    }

    private void ClosePanel()
    {
        Time.timeScale = 1f;

        if (panel != null)
        {
            panel.SetActive(false);

            // 다시 원래 상태 복구
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.enabled = true;
        }

        if (closeSkillButton != null)
            closeSkillButton.gameObject.SetActive(true);

        isSkillOpened = false;
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}