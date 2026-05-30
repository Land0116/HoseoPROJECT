using System.Collections;
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
}