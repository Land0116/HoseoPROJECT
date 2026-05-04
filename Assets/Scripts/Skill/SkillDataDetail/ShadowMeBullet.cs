using UnityEngine;

public class ShadowMeBullet : MonoBehaviour
{
    private float damage;
    private Vector3 dir;
    public float speed = 10f;

    public void Init(float dmg, Vector3 direction)
    {
        damage = dmg;
        dir = direction.normalized;
    }

    private void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            Monster m = collision.GetComponent<Monster>();

            if (m != null)
            {
                m.OnDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}