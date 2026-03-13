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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if(damageable != null)
            {
                damageable.OnDamage(damage);
            }
            Destroy(gameObject);
        }
        
    }
}
