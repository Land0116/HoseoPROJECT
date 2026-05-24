using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "AttackBoostSkill", menuName = "Game/Skill/AttackBoost")]
public class AttackBoostSkillData : SkillData
{
    public GameObject auraPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        

        int level = GetSkillLevel();

        float multiplier = GetMultiplier(level);
        float duration = GetDuration(level);

        player.StartCoroutine(BoostRoutine(player, multiplier, duration));
    }

    private IEnumerator BoostRoutine(PlayerController player, float multiplier, float duration)
    {
        // 기존 공격력 저장
        player.SetAttackMultiplier(multiplier);

        // 오라 생성
        GameObject aura = null;

        if (auraPrefab != null)
        {
            aura = Instantiate(auraPrefab, player.transform);
            aura.transform.localPosition = Vector3.zero;
        }

        yield return new WaitForSeconds(duration);

        // 원상복구
        player.SetAttackMultiplier(1f);

        if (aura != null)
            Destroy(aura);
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
            _ => 1f
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
            1 => 12f,
            2 => 11f,
            3 => 10f,
            _ => 12f
        };
    }
}