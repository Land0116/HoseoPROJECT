using UnityEngine;
using System.Collections;

public class ExplosionDroneInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private float duration;

    [SerializeField] private GameObject explosionPrefab;

    private float speed = 10f;
    private Monster target;

    public void Init(float dmg, float rng, float dur)
    {
        damage = dmg;
        range = rng;
        duration = dur;

        target = FindNearestMonster();

        StartCoroutine(LifeCycle());
    }

    private void Update()
    {
        if (target == null)
        {
            target = FindNearestMonster();
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    private IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(duration);

        Explode();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Monster"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 범위 폭발 시각화
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = Vector3.one * range;
        }

        // 데미지 처리
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

    private Monster FindNearestMonster()
    {
        Monster[] monsters = GameObject.FindObjectsByType<Monster>(FindObjectsSortMode.None);

        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (var m in monsters)
        {
            float dist = Vector3.Distance(transform.position, m.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }

        return nearest;
    }
}