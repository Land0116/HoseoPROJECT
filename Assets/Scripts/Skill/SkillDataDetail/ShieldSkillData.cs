using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "ShieldSkill", menuName = "Game/Skill/Shield")]
public class ShieldSkillData : SkillData
{
    public GameObject shieldPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();
        float duration = GetDuration(level);

        player.StartCoroutine(ShieldRoutine(player, duration));
    }

    private IEnumerator ShieldRoutine(PlayerController player, float duration)
    {
        GameObject shieldObj = null;

        if (shieldPrefab != null)
        {
            shieldObj = Instantiate(shieldPrefab, player.transform);
            shieldObj.transform.localPosition = Vector3.zero;
        }

        player.SetShieldState(true);

        yield return new WaitForSeconds(duration);

        player.SetShieldState(false);

        if (shieldObj != null)
            Destroy(shieldObj);
    }

    private int GetSkillLevel()
    {
        if (SkillManager.Instance.qSkill == this)
            return SkillManager.Instance.GetQLevel();

        if (SkillManager.Instance.eSkill == this)
            return SkillManager.Instance.GetELevel();

        return 1;
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