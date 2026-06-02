using System.Collections;
using UnityEngine;

public class SonicWave2 : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    private bool canDamage = false;
    private bool isDead = false;

    private void Start()
    {
        if (col != null)
            col.enabled = false;

        StartCoroutine(LifeRoutine());
    }

    public void Init(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator LifeRoutine()
    {
        yield return ChargeRoutine(lifeTime);

        if (this == null || isDead) yield break;

        canDamage = true;

        if (col != null)
            col.enabled = true;

        yield return new WaitForSeconds(0.1f);

        SafeDestroy();
    }

    public IEnumerator ChargeRoutine(float chargeTime)
    {
        float t = 0f;

        while (t < chargeTime)
        {
            if (this == null || isDead) yield break;
            if (sr == null) yield break;

            t += Time.deltaTime;

            float ratio = Mathf.Clamp01(t / chargeTime);

            Color c = sr.color;
            c.a = ratio;
            sr.color = c;

            yield return null;
        }

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canDamage) return;
        if (isDead) return;

        if (!collision.CompareTag("Player")) return;

        if (collision.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.OnDamage(damage);
        }

        SafeDestroy();
    }

    private void SafeDestroy()
    {
        if (isDead) return;

        isDead = true;
        Destroy(gameObject);
    }
}