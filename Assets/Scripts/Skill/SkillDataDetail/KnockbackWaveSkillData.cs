using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackWaveSkill", menuName = "Game/Skill/KnockbackWave")]
public class KnockbackWaveSkillData : SkillData
{
    public GameObject wavePrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        float damage = GetFinalDamage(player, level);
        float range = GetRange(level);

        GameObject obj = Instantiate(wavePrefab, player.transform.position, Quaternion.identity);

        KnockbackWaveInstance instance = obj.GetComponent<KnockbackWaveInstance>();

        if (instance != null)
        {
            instance.Init(damage, range, player.transform.position);
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

    public float GetRange(int level)
    {
        return level switch
        {
            1 => 3f,
            2 => 3.5f,
            3 => 4f,
            _ => 3f
        };
    }

    public override float GetCooldown(int level)
    {
        return level switch
        {
            1 => 6f,
            2 => 5.5f,
            3 => 5f,
            _ => 6f
        };
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDamage = level switch
        {
            1 => 4f,
            2 => 6f,
            3 => 8f,
            _ => 4f
        };

        float multiplier = level switch
        {
            1 => 1f,
            2 => 1.2f,
            3 => 1.5f,
            _ => 1f
        };

        return baseDamage + player.GetFinalDamage() * multiplier;
    }
}