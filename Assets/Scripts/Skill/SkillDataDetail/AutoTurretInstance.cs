using UnityEngine;
using System.Collections;

public class AutoTurretInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private float duration;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    private float attackInterval = 1f;

    public void Init(float dmg, float rng, float dur)
    {
        damage = dmg;
        range = rng;
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
        if (bulletPrefab == null) return;

        Vector3 dir = (target.transform.position - transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint != null ? firePoint.position : transform.position,
            Quaternion.identity
        );

        TurretBullet bulletScript = bullet.GetComponent<TurretBullet>();

        if (bulletScript != null)
        {
            bulletScript.Init(dir, damage);
        }
    }

    private Monster FindNearestMonster()
    {
        Monster[] monsters = GameObject.FindObjectsByType<Monster>(FindObjectsSortMode.None);

        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (var m in monsters)
        {
            float dist = Vector3.Distance(transform.position, m.transform.position);

            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }

        return nearest;
    }
}