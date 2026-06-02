using UnityEngine;
using System.Collections;

public class ShadowMeInstance : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private Vector2 lastLookDir = Vector2.down;
    private string currentAnim;
    private bool isAttacking = false;

    private float damage;
    private float duration;

    private float attackInterval = 1f;
    private float range = 15f;

    [SerializeField] private GameObject bulletPrefab;

    public void Init(float dmg, float dur)
    {
        damage = dmg;
        duration = dur;

        StartCoroutine(LifeCycle());
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    /*private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            Monster target = FindNearestMonster();

            if (target != null)
            {
                Shoot(target);
            }
        }
    }*/
    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            Monster target = FindNearestMonster();

            if (target != null)
            {
                Vector2 dir = (target.transform.position - transform.position).normalized;

                lastLookDir = dir;
                PlayAttackAnimation(dir);

                Shoot(target);
            }
            else
            {
                PlayIdleAnimation();
            }
        }
    }
    private void PlayIdleAnimation()
    {
        isAttacking = false;

        float angle = Mathf.Atan2(lastLookDir.y, lastLookDir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Idle";

        PlayAnim(anim);
    }

    private void PlayAttackAnimation(Vector2 dir)
    {
        isAttacking = true;

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Attack";

        PlayAnim(anim);
    }
    private void PlayAnim(string animName)
    {
        if (animator == null) return;

        if (currentAnim == animName) return;

        currentAnim = animName;
        animator.Play(animName);
    }
    private string GetDirectionName(float angle)
    {
        if (angle >= -22.5f && angle < 22.5f)
            return "SideR";
        if (angle >= 22.5f && angle < 67.5f)
            return "QBackR";
        if (angle >= 67.5f && angle < 112.5f)
            return "Back";
        if (angle >= 112.5f && angle < 157.5f)
            return "QBackL";
        if (angle >= 157.5f || angle < -157.5f)
            return "SideL";
        if (angle >= -157.5f && angle < -112.5f)
            return "QFrontL";
        if (angle >= -112.5f && angle < -67.5f)
            return "Front";
        if (angle >= -67.5f && angle < -22.5f)
            return "QFrontR";

        return "Front";
    }
    private void Shoot(Monster target)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        ShadowMeBullet b = bullet.GetComponent<ShadowMeBullet>();

        if (b != null)
        {
            b.Init(damage, dir);
        }
    }

    private Monster FindNearestMonster()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Monster")) continue;

            float dist = Vector2.Distance(transform.position, h.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = h.GetComponent<Monster>();
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}