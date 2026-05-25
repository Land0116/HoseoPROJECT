using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TeleportSkill", menuName = "Game/Skill/Teleport")]
public class TeleportSkillData : SkillData
{
    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();
        float range = GetRange(level);

        Vector3 mousePos = GetMouseWorldPosition();
        Vector2 dir = ((Vector2)mousePos - (Vector2)player.transform.position).normalized;

        player.StartCoroutine(TeleportRoutine(player, dir, range));
    }

    private IEnumerator TeleportRoutine(PlayerController player, Vector2 dir, float range)
    {
        float duration = 0.03f; 
        float elapsed = 0f;

        Vector3 startPos = player.transform.position;
        Vector3 endPos = startPos + (Vector3)(dir * range);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            player.transform.position = endPos;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            Vector2 newPos = Vector2.Lerp(startPos, endPos, t);

            rb.MovePosition(newPos);

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPos);
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
            1 => 6f,
            2 => 7f,
            3 => 8f,
            _ => 6f
        };
    }

    public override float GetCooldown(int level)
    {
        return level switch
        {
            1 => 5f,
            2 => 4.5f,
            3 => 4f,
            _ => 5f
        };
    }
}