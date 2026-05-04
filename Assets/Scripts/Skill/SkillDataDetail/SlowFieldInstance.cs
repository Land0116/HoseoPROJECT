using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowFieldInstance : MonoBehaviour
{
    private float range;
    private float duration;
    private float slowMultiplier;

    private Collider2D[] targets;
    private void Update()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        HashSet<Monster> current = new HashSet<Monster>();

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Monster"))
            {
                Monster m = hit.GetComponent<Monster>();
                if (m == null) continue;

                current.Add(m);

                if (!m.IsSlowedBy(this))
                    m.AddSlow(this, slowMultiplier);
            }
        }

        // 필드 밖으로 나간 몬스터 처리
        foreach (var m in FindObjectsByType<Monster>(FindObjectsSortMode.None))
        {
            if (!current.Contains(m))
            {
                m.RemoveSlow(this);
            }
        }
    }
    public void Init(float rng, float dur, float slow)
    {
        range = rng;
        duration = dur;
        slowMultiplier = slow;
        ApplyScale();
        StartCoroutine(FieldRoutine());
    }

    private IEnumerator FieldRoutine()
    {
        ApplySlow();

        yield return new WaitForSeconds(duration);

        RemoveSlow();

        Destroy(gameObject);
    }
    private void ApplyScale()
    {
        float diameter = range * 2f;

        transform.localScale = new Vector3(diameter, diameter, 1f);
    }
    private void ApplySlow()
    {
        targets = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var t in targets)
        {
            if (t.CompareTag("Monster"))
            {
                Monster monster = t.GetComponent<Monster>();

                if (monster != null)
                {
                    monster.AddSlow(this, slowMultiplier);
                }
            }
        }
    }

    private void RemoveSlow()
    {
        if (targets == null) return;

        foreach (var t in targets)
        {
            if (t == null) continue;

            Monster monster = t.GetComponent<Monster>();

            if (monster != null)
            {
                monster.RemoveSlow(this); 
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }


}