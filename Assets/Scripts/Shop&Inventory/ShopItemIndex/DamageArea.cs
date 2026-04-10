using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    public float damage = 10f;
    public float interval = 0.1f;
    public int repeatCount = 3;

    private Collider2D[] colliders;
    private SpriteRenderer[] renderers;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(DamageRoutine());
    }

    IEnumerator DamageRoutine()
    {
        for (int i = 0; i < repeatCount; i++)
        {
            SetActiveState(true);

            DealDamage();

            yield return new WaitForSeconds(interval);

            SetActiveState(false);

            yield return new WaitForSeconds(interval);
        }

        Destroy(gameObject);
    }

    void SetActiveState(bool state)
    {
        foreach (var c in colliders)
        {
            c.enabled = state;
        }

        foreach (var r in renderers)
        {
            r.enabled = state;
        }
    }

    void DealDamage()
    {
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        foreach (var col in colliders)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                col.bounds.center,
                col.bounds.size,
                0f
            );

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Monster")) continue;

                IDamageable target = hit.GetComponent<IDamageable>();

                if (target == null) continue;

                // 이미 맞은 대상이면 스킵
                if (damagedTargets.Contains(target)) continue;

                target.OnDamage(damage);
                damagedTargets.Add(target);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}