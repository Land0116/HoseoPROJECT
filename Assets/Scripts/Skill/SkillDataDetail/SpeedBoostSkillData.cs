using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SpeedSkill", menuName = "Game/Skill/Speed")]
public class SpeedBoostSkillData : SkillData
{
    public GameObject speedPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        float duration = GetDuration(level);
        float multiplier = GetMultiplier(level);

        player.StartCoroutine(SpeedRoutine(player, duration, multiplier));
    }

    private IEnumerator SpeedRoutine(PlayerController player, float duration, float multiplier)
    {
        GameObject obj = null;

        if (speedPrefab != null)
        {
            obj = Instantiate(speedPrefab, player.transform);
            obj.transform.localPosition = Vector3.zero;
        }

        player.SetSpeedBuffState(true);
        player.ApplySpeedBuffMultiplier(multiplier);

        yield return new WaitForSeconds(duration);

        player.SetSpeedBuffState(false);

        // 원복은 무조건 재빌드
        player.RebuildPlayerStats();

        if (obj != null)
            Destroy(obj);
    }

    private int GetSkillLevel()
    {
        if (SkillManager.Instance.qSkill == this)
            return SkillManager.Instance.GetQLevel();

        if (SkillManager.Instance.eSkill == this)
            return SkillManager.Instance.GetELevel();

        return 1;
    }
    private float GetMultiplier(int level)
    {
        return level switch
        {
            1 => 1.5f,
            2 => 1.7f,
            3 => 2f,
            _ => 1.5f
        };
    }
    private float GetDuration(int level)
    {
        return level switch
        {
            1 => 5f,
            2 => 6f,
            3 => 7f,
            _ => 5f
        };
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
}