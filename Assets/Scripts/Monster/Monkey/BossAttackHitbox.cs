using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackHitbox : MonoBehaviour
{
    private float damage;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    [SerializeField] private float fadeTime = 0.3f;

    private SpriteRenderer sr;
    private Collider2D col;

    private bool canDamage = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;
    }

    public void SetDamage(float value)
    {
        damage = value;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (sr == null)
            yield break;

        float t = 0f;

        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = t / fadeTime;

            c.a = alpha;
            sr.color = c;

            yield return null;
        }

        // 완전 선명해지는 순간
        col.enabled = true;
        canDamage = true;

        // 짧은 시간만 데미지 허용
        yield return new WaitForSeconds(0.1f);

        canDamage = false;

        Destroy(gameObject, 0.2f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canDamage) return;
        if (!collision.CompareTag("Player")) return;

        IDamageable target = collision.GetComponent<IDamageable>();
        if (target == null) return;

        if (hitTargets.Contains(target)) return;

        hitTargets.Add(target);
        target.OnDamage(damage);
    }
}