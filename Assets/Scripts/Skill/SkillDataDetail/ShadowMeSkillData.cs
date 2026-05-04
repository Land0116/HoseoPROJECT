using UnityEngine;

[CreateAssetMenu(fileName = "ShadowMe", menuName = "Game/Skill/ShadowMe")]
public class ShadowMeSkillData : SkillData
{
    public GameObject turretPrefab;

    public override void Execute(GameObject caster)
    {
        int level = GetSkillLevel();

        Vector3 spawnPos = caster.transform.position + GetRandomOffset();

        GameObject obj = Instantiate(turretPrefab, spawnPos, Quaternion.identity);

        ShadowMeInstance turret = obj.GetComponent<ShadowMeInstance>();

        if (turret != null)
        {
            turret.Init(
                GetFinalDamage(PlayerController.Instance, level),
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
            1 => 10f,
            2 => 10f,
            3 => 9f,
            _ => 10f
        };
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDmg = level switch
        {
            1 => 7f,
            2 => 9f,
            3 => 12f,
            _ => 7f
        };

        float multiplier = level switch
        {
            1 => 0.7f,
            2 => 0.9f,
            3 => 1.1f,
            _ => 0.7f
        };

        return baseDmg + player.GetFinalDamage() * multiplier;
    }

    public float GetDuration(int level)
    {
        return level switch
        {
            1 => 8f,
            2 => 10f,
            3 => 12f,
            _ => 8f
        };
    }
}