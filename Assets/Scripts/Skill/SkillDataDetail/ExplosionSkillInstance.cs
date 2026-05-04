using UnityEngine;

public class ExplosionSkillInstance : MonoBehaviour
{
    private float damage;
    private float range;

    public GameObject explosionFX; // 범위 표시용 (원형 이미지 or sprite)

    public void Init(float dmg, float rng, float dur)
    {
        damage = dmg;
        range = rng;

        ApplyScaleByCollider();

        Explode();
    }

    private void Explode()
    {
        // 1. 데미지 즉시 적용
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Monster"))
            {
                Monster monster = hit.GetComponent<Monster>();

                if (monster != null)
                {
                    monster.OnDamage(damage);
                }
            }
        }

        // 2. 범위 표시 FX 생성
        if (explosionFX != null)
        {
            GameObject fx = Instantiate(explosionFX, transform.position, Quaternion.identity);

            // 3. 0.2초 뒤 삭제 (핵심)
            Destroy(fx, 0.2f);
        }

        // 4. 스킬 본체 즉시 삭제 (이게 중요)
        Destroy(gameObject);
    }

    private void ApplyScaleByCollider()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();

        if (col == null) return;

        float targetDiameter = range * 2f;
        float currentDiameter = col.radius * 2f;

        float scale = targetDiameter / currentDiameter;

        transform.localScale = Vector3.one * scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}