using UnityEngine;

[CreateAssetMenu(fileName = "PullSkill", menuName = "Game/Skill/Pull")]
public class PullSkillData : SkillData
{
    public GameObject pullPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();
        float range = GetRange(level);

        Vector3 mousePos = GetMouseWorldPosition();

        GameObject obj = Instantiate(pullPrefab, mousePos, Quaternion.identity);

        PullFieldInstance instance = obj.GetComponent<PullFieldInstance>();
        if (instance != null)
        {
            instance.Init(range);
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
            1 => 4f,
            2 => 5f,
            3 => 6f,
            _ => 4f
        };
    }

    public override float GetCooldown(int level)
    {
        return level switch
        {
            1 => 9f,
            2 => 8.5f,
            3 => 7f,
            _ => 9f
        };
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        return 0f;
    }
}