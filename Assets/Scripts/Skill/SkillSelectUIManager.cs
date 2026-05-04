using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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
        Instance = this;
    }

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        isSkillOpened = false;
    }



    public void BindSkillUI(GameObject systemUIRoot, GameObject overlay)
    {
        Debug.Log("BindSkillUI 실행됨");

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
        Debug.Log("패널 열기 시도");
        Debug.Log(SkillSelectUIManager.Instance);
        if (panel == null)
        {
            Debug.LogError("Skill Panel NULL임");
            return;
        }

        panel.SetActive(true);
        Time.timeScale = 0f;

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
    }

    public void SelectSkill(SkillData skill)
    {
        selectedSkill = skill;
        state = SelectState.SkillSelected;

        Debug.Log("스킬 선택됨: " + skill.skillName);

        bool isNewSkill =
            SkillManager.Instance.qSkill != skill &&
            SkillManager.Instance.eSkill != skill;

        //신규 스킬일 때만 오버레이 ON
        if (overlayUI != null)
            overlayUI.SetActive(isNewSkill);

        // 이미 장착된 스킬이면 바로 업그레이드 처리
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

        // 신규면 Q/E 선택 대기 유지
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

        ClosePanel();
    }

    private void CancelSelection()
    {
        selectedSkill = null;

        if (overlayUI != null)
            overlayUI.SetActive(false);

        ClosePanel();
    }

    private void ClosePanel()
    {
        //Debug.Log("패널 닫기 실행됨");
        Time.timeScale = 1f;

        if (panel != null)
            panel.SetActive(false);

        isSkillOpened = false;
    }

}

