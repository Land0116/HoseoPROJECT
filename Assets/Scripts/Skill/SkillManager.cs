using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public SkillData qSkill;
    public SkillData eSkill;

    private int qLevel = 0;
    private int eLevel = 0;
    private float qLastUseTime = -999f;
    private float eLastUseTime = -999f;


    public void EquipSkill(SkillData skill, SkillSlotType slot)
    {
        if (slot == SkillSlotType.Q)
        {
            if (qSkill == skill)
            {
                if (qLevel < skill.maxLevel)
                    qLevel++;
                return;
            }

            qSkill = skill;
            qLevel = 1;
        }
        else
        {
            if (eSkill == skill)
            {
                if (eLevel < skill.maxLevel)
                    eLevel++;
                return;
            }

            eSkill = skill;
            eLevel = 1;
        }
        RefreshSlotUI();
    }

    public void UseQ()
    {
        if (qSkill == null) return;

        int level = GetQLevel();
        float cooldown = qSkill.GetCooldown(level);

        // 쿨타임 체크
        if (Time.time < qLastUseTime + cooldown)
        {
            Debug.Log("Q 쿨타임 중");
            return;
        }

        Debug.Log("Q 스킬 실행");

        qLastUseTime = Time.time;

        qSkill.Execute(PlayerController.Instance.gameObject);
    }

    public void UseE()
    {
        if (eSkill == null) return;

        int level = GetELevel();
        float cooldown = eSkill.GetCooldown(level);

        if (Time.time < eLastUseTime + cooldown)
        {
            Debug.Log("E 쿨타임 중");
            return;
        }

        Debug.Log("E 스킬 실행");

        eLastUseTime = Time.time;

        eSkill.Execute(PlayerController.Instance.gameObject);
    }

    public void RefreshSlotUI()
    {
        SkillSlotButton[] slots = FindObjectsByType<SkillSlotButton>(FindObjectsSortMode.None);

        foreach (var slot in slots)
        {
            slot.UpdateIcon();
        }
    }
    /*public bool IsQOnCooldown()
    {
        return Time.time < qLastUseTime + qCooldown;
    }

    public float GetQCooldownRemain()
    {
        float remain = (qLastUseTime + qCooldown) - Time.time;
        return Mathf.Max(0f, remain);
    }

    public float GetQCooldownRatio()
    {
        float remain = GetQCooldownRemain();
        return remain / qCooldown;
    }
    public bool IsEOnCooldown()
    {
        return Time.time < eLastUseTime + eCooldown;
    }

    public float GetECooldownRemain()
    {
        float remain = (eLastUseTime + eCooldown) - Time.time;
        return Mathf.Max(0f, remain);
    }

    public float GetECooldownRatio()
    {
        float remain = GetECooldownRemain();
        return remain / eCooldown;
    }*/
    public bool IsQOnCooldown()
    {
        if (qSkill == null) return false;

        float cooldown = qSkill.GetCooldown(qLevel);
        return Time.time < qLastUseTime + cooldown;
    }
    public float GetQCooldownRemain()
    {
        if (qSkill == null) return 0f;

        float cooldown = qSkill.GetCooldown(qLevel);
        float remain = (qLastUseTime + cooldown) - Time.time;

        return Mathf.Max(0f, remain);
    }
    public float GetQCooldownRatio()
    {
        if (qSkill == null) return 0f;

        float cooldown = qSkill.GetCooldown(qLevel);
        float remain = GetQCooldownRemain();

        return remain / cooldown;
    }
    public bool IsEOnCooldown()
    {
        if (eSkill == null) return false;

        float cooldown = eSkill.GetCooldown(eLevel);
        return Time.time < eLastUseTime + cooldown;
    }

    public float GetECooldownRemain()
    {
        if (eSkill == null) return 0f;

        float cooldown = eSkill.GetCooldown(eLevel);
        float remain = (eLastUseTime + cooldown) - Time.time;

        return Mathf.Max(0f, remain);
    }

    public float GetECooldownRatio()
    {
        if (eSkill == null) return 0f;

        float cooldown = eSkill.GetCooldown(eLevel);
        float remain = GetECooldownRemain();

        return remain / cooldown;
    }
    public int GetSkillLevel(SkillData skill)
    {
        if (qSkill == skill) return qLevel;
        if (eSkill == skill) return eLevel;
        return 1;
    }

    public int GetQLevel()
    {
        return qLevel;
    }

    public int GetELevel()
    {
        return eLevel;
    }
}

public enum SkillSlotType
{
    Q,
    E
}
