/*using System.Collections;
using UnityEngine;

public class EightDirectionHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float fadeTime = 0.7f;

    private SpriteRenderer sr;
    private bool damageApplied = false;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(FadeInAndDamage());

        Destroy(gameObject, fadeTime + 0.1f);
    }

    private IEnumerator FadeInAndDamage()
    {
        float t = 0f;

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = (t / fadeTime);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            yield return null;
        }

        ApplyDamageOnce();
    }

    private void ApplyDamageOnce()
    {
        if (damageApplied) return;

        damageApplied = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.OnDamage(damage);
                }
                break;
            }
        }
    }

    public void SetDamage(float value)
    {
        damage = value;
    }
}*/using UnityEngine;

public class EightDirectionHitbox : MonoBehaviour
{
    private float damage;
    [SerializeField] private float knockback = 5f;

    private bool hasHit = false; 

    private void Start()
    {
        Destroy(gameObject, 0.5f); 
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        if (!collision.CompareTag("Player")) return;

        hasHit = true;

        // 데미지 적용
        IDamageable dmg = collision.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.OnDamage(damage);
        }

        // 넉백
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector2 dir = (collision.transform.position - transform.position).normalized;
            player.ApplyKnockback(dir * knockback);
        }
    }
}