using UnityEngine;

[CreateAssetMenu(fileName = "LaserSkill", menuName = "Game/Skill/Laser")]
public class LaserSkillData : SkillData
{
    public GameObject laserPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        float damage = GetFinalDamage(player, level);
        float range = GetRange(level);

        Vector3 startPos = caster.transform.position;

        Vector3 targetPos = GetMouseWorldPosition();

        Vector3 direction = (targetPos - startPos).normalized;

        GameObject laser = Instantiate(laserPrefab, startPos, Quaternion.identity);

        LaserSkillInstance instance = laser.GetComponent<LaserSkillInstance>();

        if (instance != null)
        {
            instance.Init(damage, range, direction);
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

    // ===== 레벨별 값 =====

    public override float GetCooldown(int level)
    {
        switch (level)
        {
            case 1: return 6f;
            case 2: return 5.5f;
            case 3: return 5f;
        }
        return 6f;
    }

    public override float GetDamageMultiplier(int level)
    {
        switch (level)
        {
            case 1: return 2.5f;
            case 2: return 2.8f;
            case 3: return 3.2f;
        }
        return 2.5f;
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDamage = level switch
        {
            1 => 25f,
            2 => 30f,
            3 => 35f,
            _ => 25f
        };

        return baseDamage + player.GetFinalDamage() * GetDamageMultiplier(level);
    }

    public float GetRange(int level)
    {
        return level switch
        {
            1 => 6f,
            2 => 7f,
            3 => 8f,
            _ => 6f
        };
    }
}