using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    private SkillData skill;
    private SkillSelectUIManager manager;

    public void Setup(SkillData data, SkillSelectUIManager uiManager)
    {
        skill = data;
        manager = uiManager;

        int currentLevel = 0;

        if (SkillManager.Instance.qSkill == data)
            currentLevel = SkillManager.Instance.GetQLevel();
        else if (SkillManager.Instance.eSkill == data)
            currentLevel = SkillManager.Instance.GetELevel();

        int nextLevel = Mathf.Min(currentLevel + 1, data.maxLevel);

        if (currentLevel == 0)
            nameText.text = $"{data.skillName} Lv.1";
        else
            nameText.text = $"{data.skillName} Lv.{currentLevel}";

        iconImage.sprite = data.icon;
        iconImage.enabled = true;

        PlayerController player = PlayerController.Instance;

        if (currentLevel == 0)
        {
            float damage = data.GetFinalDamage(player, 1);
            float cooldown = data.GetCooldown(1);

            descText.text =
                $"{data.description}\n\n" +
                $"대미지: {damage}\n" +
                $"쿨타임: {cooldown}초";
        }
        else if (currentLevel >= data.maxLevel)
        {
            float damage = data.GetFinalDamage(player, currentLevel);
            float cooldown = data.GetCooldown(currentLevel);

            descText.text =
                $"{data.description}\n\n" +
                $"대미지: {damage}\n" +
                $"쿨타임: {cooldown}초\n" +
                $"(MAX)";
        }
        else
        {
            float currentDamage = data.GetFinalDamage(player, currentLevel);
            float nextDamage = data.GetFinalDamage(player, nextLevel);

            float currentCooldown = data.GetCooldown(currentLevel);
            float nextCooldown = data.GetCooldown(nextLevel);

            descText.text =
                $"{data.description}\n\n" +
                $"대미지: {currentDamage} → {nextDamage}\n" +
                $"쿨타임: {currentCooldown} → {nextCooldown}초";
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => manager.SelectSkill(skill));
    }

    public void SetupReplaceMode(int index, SkillSelectUIManager manager)
    {
        button.onClick.RemoveAllListeners();

        iconImage.enabled = false;
        descText.text = "";

        if (index == 0)
        {
            nameText.text = "Q 슬롯 교체";
            button.onClick.AddListener(() => manager.ReplaceSkill(SkillSlotType.Q));
        }
        else
        {
            nameText.text = "E 슬롯 교체";
            button.onClick.AddListener(() => manager.ReplaceSkill(SkillSlotType.E));
        }
    }
}