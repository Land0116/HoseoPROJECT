using UnityEngine;

public class MonsterBullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 7f;
    public int damage = 10;
    private Vector3 direction;
    private void Start()
    {
        Destroy(gameObject, 3f);
    }
    public void SetDirection(Vector3 dir)
    {
        direction = dir;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }

        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.OnDamage(damage);
                Debug.Log("플레이어 체력 감소");
            }

            Destroy(gameObject);
        }
    }
}
