using UnityEngine;

public class TurretBullet : MonoBehaviour
{
    private float damage;
    private Vector3 direction;
    private float speed = 10f;

    public void Init(Vector3 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;

        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Monster"))
        {
            Monster m = col.GetComponent<Monster>();
            if (m != null)
                m.OnDamage(damage);

            Destroy(gameObject);
        }
    }
}