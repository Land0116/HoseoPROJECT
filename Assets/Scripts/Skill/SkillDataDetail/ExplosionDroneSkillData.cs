using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionDroneSkill", menuName = "Game/Skill/ExplosionDrone")]
public class ExplosionDroneSkillData : SkillData
{
    public GameObject dronePrefab;

    public override void Execute(GameObject caster)
    {
        int level = GetSkillLevel();

        Vector3 spawnPos = caster.transform.position + GetRandomOffset();

        GameObject obj = Instantiate(dronePrefab, spawnPos, Quaternion.identity);

        ExplosionDroneInstance drone = obj.GetComponent<ExplosionDroneInstance>();

        if (drone != null)
        {
            drone.Init(
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
            1 => 8f,
            2 => 7.5f,
            3 => 7f,
            _ => 8f
        };
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        float baseDamage = level switch
        {
            1 => 22f,
            2 => 26f,
            3 => 30f,
            _ => 22f
        };

        float multiplier = level switch
        {
            1 => 2.2f,
            2 => 2.5f,
            3 => 2.8f,
            _ => 2.2f
        };

        return baseDamage + player.GetFinalDamage() * multiplier;
    }

    public float GetRange(int level)
    {
        return level switch
        {
            1 => 2f,
            2 => 2.5f,
            3 => 3f,
            _ => 2f
        };
    }

    public float GetDuration(int level)
    {
        return level switch
        {
            1 => 2f,
            2 => 3f,
            3 => 4f,
            _ => 2f
        };
    }
}