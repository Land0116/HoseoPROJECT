using UnityEngine;
using System.Collections;

public class ShadowMeInstance : MonoBehaviour
{
    private float damage;
    private float duration;

    private float attackInterval = 1f;
    private float range = 5f;

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

    private IEnumerator AttackRoutine()
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