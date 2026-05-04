using UnityEngine;
using System.Collections;
public class KnockbackWaveInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private Vector2 origin;

    [SerializeField] private float knockbackForce = 8f;

    public void Init(float dmg, float rng, Vector2 originPos)
    {
        damage = dmg;
        range = rng;
        origin = originPos;
        ApplyScale();
        Execute();
    }
    private void ApplyScale()
    {
        float diameter = range * 2f;

        transform.localScale = new Vector3(diameter, diameter, 1f);
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = 0.5f; 
        }
    }
    private void Execute()
    {
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Monster")) continue;

            Monster monster = hit.GetComponent<Monster>();
            if (monster == null) continue;

            monster.OnDamage(damage);

            Vector2 dir = (hit.transform.position - transform.position).normalized;

            Rigidbody2D rb = monster.RB;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        // 여기 핵심: 0.2초 유지
        yield return new WaitForSeconds(0.2f);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}