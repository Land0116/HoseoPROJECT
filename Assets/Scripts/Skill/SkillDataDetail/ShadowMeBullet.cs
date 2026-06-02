using UnityEngine;

public class ShadowMeBullet : MonoBehaviour
{
    private float damage;
    private Vector3 dir;
    public float speed = 10f;

    private Animator animator;
    private bool isDestroyed = false;

    public void Init(float dmg, Vector3 direction)
    {
        damage = dmg;
        dir = direction.normalized;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.Play("BulletAttack", 0, 0f);
        }
    }

    private void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed) return;

        if (collision.CompareTag("Monster"))
        {
            Monster m = collision.GetComponent<Monster>();

            if (m != null)
            {
                m.OnDamage(damage);
            }

            PlayDestroy();
        }
    }

    private void PlayDestroy()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        speed = 0f;

        if (animator != null)
        {
            animator.Play("BulletFinishAttack", 0, 0f);
        }

        Destroy(gameObject, 0.3f);
    }
}