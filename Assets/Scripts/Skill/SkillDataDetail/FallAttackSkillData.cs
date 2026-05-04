using UnityEngine;

[CreateAssetMenu(fileName = "FallAttackSkill", menuName = "Game/Skill/FallAttack")]
public class FallAttackSkillData : SkillData
{
    public GameObject warningPrefab;

    public override void Execute(GameObject caster)
    {
        int level = GetSkillLevel();

        Vector3 pos = GetMouseWorldPosition();

        GameObject obj = Instantiate(warningPrefab, pos, Quaternion.identity);

        FallAttackInstance instance = obj.GetComponent<FallAttackInstance>();

        if (instance != null)
        {
            instance.Init(
                GetFinalDamage(PlayerController.Instance, level),
                GetRange(level),
                GetCooldown(level)
            );
        }
    }

    private int GetSkillLevel()
    {
        if (SkillManager.Instance.qSkill == this)
            return SkillManager.Instance.GetQLevel();

        if (SkillManager.Instance.eSkill == this)
            return SkillManager.Instance.GetELevel();

        return 1;
    }

    public override float GetCooldown(int level)
    {
        switch (level)
        {
            case 1: return 7f;
            case 2: return 6.5f;
            case 3: return 6f;
        }
        return 7f;
    }

    public float GetRange(int level)
    {
        switch (level)
        {
            case 1: return 4f;
            case 2: return 4.5f;
            case 3: return 5f;
        }
        return 4f;
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDmg = level switch
        {
            1 => 18f,
            2 => 22f,
            3 => 26f,
            _ => 18f
        };

        float multiplier = level switch
        {
            1 => 1.8f,
            2 => 2f,
            3 => 2.3f,
            _ => 1.8f
        };

        return baseDmg + player.GetFinalDamage() * multiplier;
    }
}