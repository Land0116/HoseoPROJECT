using UnityEngine;

public class LaserSkillInstance : MonoBehaviour
{
    private float damage;
    private float range;
    private Vector3 direction;

    public float width = 0.3f;

    public void Init(float dmg, float rng, Vector3 dir)
    {
        damage = dmg;
        range = rng;
        direction = dir.normalized;

        TransformLaser();
        HitCheck();

        Destroy(gameObject, 0.1f);
    }

    private void TransformLaser()
    {
        // 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 스케일
        transform.localScale = new Vector3(width, range, 1f);

        //위치를 절반 앞으로 이동
        transform.position += direction * (range * 0.5f);
    }

    private void HitCheck()
    {
        Vector2 start = transform.position;
        Vector2 end = start + (Vector2)direction * range;

        RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, range);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Monster"))
            {
                Monster monster = hit.collider.GetComponent<Monster>();

                if (monster != null)
                {
                    monster.OnDamage(damage);
                }
            }
        }
    }
}