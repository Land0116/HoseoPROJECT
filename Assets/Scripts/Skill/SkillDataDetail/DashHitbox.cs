using System.Collections;
using UnityEngine;

public class DashHitbox : MonoBehaviour
{
    private float damage;

    public void Init(float dmg, float range, Vector2 dir)
    {
        damage = dmg;

        ApplyScale(range, dir);

        StartCoroutine(LifeTime());
    }

    private void ApplyScale(float range, Vector2 dir)
    {

        float length = range; // 앞쪽 거리

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 핵심: Y(세로) = 옆 범위 증가
        float sideWidth = 1.5f; // ← 여기 조절 (기본 0.6 → 1.5)

        transform.localScale = new Vector3(length, sideWidth, 1f);

        transform.position -= (Vector3)(dir * (length * 0.5f));//*
    }

    private IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Monster")) return;

        Monster monster = other.GetComponent<Monster>();
        if (monster == null) return;

        monster.OnDamage(damage);
    }
}