using UnityEngine;
using UnityEngine.UI;

public class SkillSlotButton : MonoBehaviour
{
    public SkillSlotType slotType;
    private Button button;
    [SerializeField] private Image iconImage;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log("½½·Ô ¹öÆ° Å¬¸¯µÊ: " + slotType);

        SkillSelectUIManager.Instance.AssignToSlot(slotType);

        UpdateIcon();
    }
    public void UpdateIcon()
    {
        SkillData skill = null;

        if (slotType == SkillSlotType.Q)
            skill = SkillManager.Instance.qSkill;
        else
            skill = SkillManager.Instance.eSkill;

        if (skill != null)
        {
            iconImage.sprite = skill.sloticon; //*
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
    }


}