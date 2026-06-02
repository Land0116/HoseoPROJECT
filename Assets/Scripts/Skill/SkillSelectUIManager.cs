using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class SkillSelectUIManager : MonoBehaviour
{
    public static SkillSelectUIManager Instance;

    [Header("������")]
    public SkillData[] skillDatabase;

    [Header("UI")]
    public GameObject panel;
    public SkillSelectButton[] buttons;
    [SerializeField] private Button closeSkillButton;

    private SkillData selectedSkill;
    private bool isSkillOpened = false;
    private Action onRewardSkillFinished;
    private bool isRewardMode = false;

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

    public void OpenSkillPanelFromReward(Action rewardFinishedCallback)
    {
        onRewardSkillFinished = rewardFinishedCallback;
        isRewardMode = true;
        isShopMode = false;

        if (panel == null)
        {
            Debug.LogWarning("[SkillSelectUIManager] SkillSelectPanel이 바인딩되지 않음. UIManager.BindSkillUI 확인 필요.");

            isRewardMode = false;

            Action callback = onRewardSkillFinished;
            onRewardSkillFinished = null;
            callback?.Invoke();
            return;
        }

        if (skillDatabase == null || skillDatabase.Length == 0)
        {
            Debug.LogWarning("[SkillSelectUIManager] skillDatabase가 비어 있음. 스킬 보상 완료 처리.");

            isRewardMode = false;

            Action callback = onRewardSkillFinished;
            onRewardSkillFinished = null;
            callback?.Invoke();
            return;
        }
        
        IsSkillOpened = true;
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

            //Transform closeBtn = UIManager.FindChildRecursive(root, "CloseSkillPanelButton");

            //if (closeBtn != null)
            //{
            //    closeSkillButton = closeBtn.GetComponent<Button>();
            //    closeSkillButton.onClick.RemoveAllListeners();
            //    closeSkillButton.onClick.AddListener(CancelSelection);
            //}
        }

        

    }

    private void OpenPanel()
    {
        if (isShopMode) return;

        if (panel == null)
        {
            Debug.LogWarning("[SkillSelectUIManager] panel == null. SkillSelectPanel 바인딩 실패.");
            return;
        }
        
        panel.SetActive(true);
        Time.timeScale = 0f;

        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.SkillPanel, true);
         if (closeSkillButton != null)
         {
             // 보상으로 열린 스킬 선택은 취소하면 문이 안 열리는 문제가 생긴다.
             // 따라서 보상 모드에서는 닫기 버튼을 숨긴다.
             closeSkillButton.gameObject.SetActive(!isRewardMode);
         }

        /*List<SkillData> randomSkills = skillDatabase
            .OrderBy(x => Random.value)
            .Take(2)
            .ToList();*/
        List<SkillData> randomSkills = skillDatabase
    .Where(s => s != SkillManager.Instance.qSkill &&
                s != SkillManager.Instance.eSkill)
    .OrderBy(x => Random.value)
    .Distinct()
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

        // panel�ȿ���
        if (panel != null)
            panel.SetActive(false);

        // overlay�� �Ѽ� Q/E Ŭ�� ����
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

        PlayerController.Instance?.SetSystemInputLockedByKey(InputLockKeys.SkillPanel, false);

        if (panel != null)
        {
            panel.SetActive(false);

            // �ٽ� ���� ���� ����
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.enabled = true;
        }

        if (closeSkillButton != null)
            closeSkillButton.gameObject.SetActive(true);

        isSkillOpened = false;
        
        if (isRewardMode)
        {
            isRewardMode = false;

            Action callback = onRewardSkillFinished;
            onRewardSkillFinished = null;

            callback?.Invoke();
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}