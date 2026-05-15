using UnityEngine;
using System.Collections.Generic;

public class BossAttackHitbox : MonoBehaviour
{
    private float damage;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void SetDamage(float value)
    {
        damage = value;
        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        IDamageable target = collision.GetComponent<IDamageable>();
        if (target == null) return;

        if (hitTargets.Contains(target)) return;

        hitTargets.Add(target);
        target.OnDamage(damage);
    }
}