using UnityEngine;

[CreateAssetMenu(fileName = "AutoTurretSkill", menuName = "Game/Skill/AutoTurret")]
public class AutoTurretSkillData : SkillData
{
    public GameObject turretPrefab;

    public override void Execute(GameObject caster)
    {
        int level = GetSkillLevel();

        Vector3 spawnPos = caster.transform.position + GetRandomOffset();

        GameObject obj = Instantiate(turretPrefab, spawnPos, Quaternion.identity);

        AutoTurretInstance turret = obj.GetComponent<AutoTurretInstance>();

        if (turret != null)
        {
            turret.Init(
                GetFinalDamage(PlayerController.Instance, level),
                GetRange(level),
                GetDuration(level)
            );
        }
    }

    private Vector3 GetRandomOffset()
    {
        Vector2 rand = Random.insideUnitCircle * 1f;
        return new Vector3(rand.x, rand.y, 0f);
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
        return level switch
        {
            1 => 12f,
            2 => 11f,
            3 => 10f,
            _ => 12f
        };
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDamage = level switch
        {
            1 => 6f,
            2 => 8f,
            3 => 10f,
            _ => 6f
        };

        float multiplier = level switch
        {
            1 => 0.6f,
            2 => 0.8f,
            3 => 1f,
            _ => 0.6f
        };

        return baseDamage + player.GetFinalDamage() * multiplier;
    }

    public float GetRange(int level)
    {
        return level switch
        {
            1 => 5f,
            2 => 6f,
            3 => 7f,
            _ => 5f
        };
    }

    public float GetDuration(int level)
    {
        return level switch
        {
            1 => 10f,
            2 => 12f,
            3 => 15f,
            _ => 10f
        };
    }
}