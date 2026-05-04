using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "DashAttackSkill", menuName = "Game/Skill/DashAttack")]
public class DashAttackSkillData : SkillData
{
    public GameObject hitboxPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        float damage = GetDamage(level, player);
        float range = GetRange(level);
        

        Vector3 mousePos = GetMouseWorldPosition();
        Vector2 dir = ((Vector2)mousePos - (Vector2)player.transform.position).normalized;

        player.StartCoroutine(DashRoutine(player, dir, range, damage));
    }

    
    private IEnumerator DashRoutine(PlayerController player, Vector2 dir, float range, float damage)
    {
        Vector3 startPos = player.transform.position;

        Vector3 centerPos = startPos + (Vector3)(dir * (range * 0.75f));

        GameObject obj = Instantiate(hitboxPrefab, centerPos, Quaternion.identity);

        DashHitbox hitbox = obj.GetComponent<DashHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(damage, range, dir);
        }

        float moved = 0f;
        float speed = 50f;

        while (moved < range)
        {
            float step = speed * Time.deltaTime;

            if (moved + step > range)
                step = range - moved;

            player.transform.position += (Vector3)(dir * step);

            moved += step;
            yield return null;
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
            1 => 4f,
            2 => 3.5f,
            3 => 3f,
            _ => 4f
        };
    }

    public float GetDamage(int level, PlayerController player)
    {
        float baseDamage = level switch
        {
            1 => 15f,
            2 => 18f,
            3 => 22f,
            _ => 15f
        };

        float multiplier = level switch
        {
            1 => 1.5f,
            2 => 1.8f,
            3 => 2.2f,
            _ => 1.5f
        };

        return baseDamage + player.GetFinalDamage() * multiplier;
    }
}