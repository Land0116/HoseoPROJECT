using System.Collections;
using UnityEngine;

public class PullFieldInstance : MonoBehaviour
{
    private float range;
    [SerializeField] private float pullSpeed = 8f;
    [SerializeField] private float duration = 0.3f;

    public void Init(float rng)
    {
        range = rng;

        // 시각적으로 범위 맞추기
        float diameter = range * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);

        StartCoroutine(PullRoutine());
    }

    private IEnumerator PullRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            ApplyPull();
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyPull()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Monster")) continue;

            Monster monster = hit.GetComponent<Monster>();
            if (monster == null) continue;

            Rigidbody2D rb = monster.RB;
            if (rb == null) continue;

            Vector2 dir = ((Vector2)transform.position - rb.position).normalized;

            rb.AddForce(dir * pullSpeed, ForceMode2D.Force);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}