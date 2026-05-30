/*using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackHitbox : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

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

        // ===== 추가된 부분 =====
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            // 현재 히트박스 크기 그대로 맞추기
            effect.transform.localScale = transform.localScale;
        }
        // ======================

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
}*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackHitbox : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

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

            // 최대 50%까지만
            c.a = Mathf.Lerp(0f, 0.5f, alpha);

            sr.color = c;

            yield return null;
        }

        // ===== 완전히 선명해지는 순간 =====

        col.enabled = true;
        canDamage = true;

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            // 히트박스 크기 그대로 복사
            effect.transform.localScale = transform.localScale;
        }

        // 즉시 자신 제거 (히트박스 안 보이게)
        yield return null;

        Destroy(gameObject);

        // ==================================
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