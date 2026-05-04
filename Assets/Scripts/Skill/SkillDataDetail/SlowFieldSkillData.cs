using UnityEngine;

[CreateAssetMenu(fileName = "SlowFieldSkill", menuName = "Game/Skill/SlowField")]
public class SlowFieldSkillData : SkillData
{
    public GameObject slowFieldPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        Vector3 pos = GetMouseWorldPosition();

        GameObject obj = Instantiate(slowFieldPrefab, pos, Quaternion.identity);

        SlowFieldInstance instance = obj.GetComponent<SlowFieldInstance>();

        if (instance != null)
        {
            instance.Init(
                GetRange(level),
                GetDuration(level),
                GetSlowMultiplier(level)
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
        return level switch
        {
            1 => 10f,
            2 => 9f,
            3 => 8f,
            _ => 10f
        };
    }

    public float GetRange(int level)
    {
        return level switch
        {
            1 => 4f,
            2 => 5f,
            3 => 6f,
            _ => 4f
        };
    }

    public float GetDuration(int level)
    {
        return level switch
        {
            1 => 5f,
            2 => 6f,
            3 => 7f,
            _ => 5f
        };
    }

    public float GetSlowMultiplier(int level)
    {
        return level switch
        {
            1 => 0.6f,
            2 => 0.5f,
            3 => 0.4f,
            _ => 0.6f
        };
    }
}