/*using UnityEngine;

public class FallAttackInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private float delay = 2f;

    [SerializeField] private SpriteRenderer warningSprite;
    [SerializeField] private CircleCollider2D col;

    public void Init(float dmg, float rng, float cd)
    {
        damage = dmg;
        range = rng;

        transform.localScale = Vector3.one * range;

        StartCoroutine(FallRoutine());
    }

    private System.Collections.IEnumerator FallRoutine()
    {
        float t = 0f;

        Color c = warningSprite.color;
        c.a = 0f;
        warningSprite.color = c;

        while (t < delay)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Clamp01(t / delay);

            c.a = alpha;
            warningSprite.color = c;

            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Monster"))
            {
                Monster m = hit.GetComponent<Monster>();
                if (m != null)
                    m.OnDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}*/
using UnityEngine;
using System.Collections;

public class FallAttackInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private float delay = 2f;

    private GameObject attackPrefab; // 추가

    [SerializeField] private SpriteRenderer warningSprite;

    public void Init(float dmg, float rng, float cd)
    {
        damage = dmg;
        range = rng;

        transform.localScale = Vector3.one * range;

        StartCoroutine(FallRoutine());
    }

    //  attackPrefab 따로 세팅하는 함수
    public void SetAttackPrefab(GameObject prefab)
    {
        attackPrefab = prefab;
    }

    private IEnumerator FallRoutine()
    {
        float t = 0f;

        Color c = warningSprite.color;
        c.a = 0f;
        warningSprite.color = c;

        while (t < delay)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Clamp01(t / delay);
            c.a = alpha;
            warningSprite.color = c;

            yield return null;
        }

        //  공격 생성 (여기로 이동됨)
        if (attackPrefab != null)
        {
            GameObject atk = Instantiate(attackPrefab, transform.position, Quaternion.identity);
            atk.transform.localScale = Vector3.one * range;

            FallAttackDamage dmgComp = atk.GetComponent<FallAttackDamage>();
            if (dmgComp != null)
            {
                dmgComp.Init(damage);
            }
        }

        Destroy(gameObject);
    }
}