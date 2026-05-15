using UnityEngine;

public class RobotBullet : MonoBehaviour
{
    private Vector2 velocity;

    [Header("Speed Settings")]
    [SerializeField] private float startSpeed = 8f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float acceleration = 10f;

    [Header("Arc Settings")]
    [SerializeField] private float arcHeight = 5f;
    [SerializeField] private float gravity = -20f;
    //[SerializeField] private float gravity = +20f;
    private float currentSpeed;

    [Header("Combat")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockback = 5f;

    [Header("LifeTime")]
    [SerializeField] private float lifeTime = 5f;


    private Vector2 startPos;
    private Vector2 targetPos;
    private float t;
    private static int arcToggle = 1;
    private float currentArc;

    public void Init(Vector2 dir, float dmg, float kb, float spd, Transform target)
    {
        damage = dmg;
        knockback = kb;
        startSpeed = spd;
        currentSpeed = startSpeed;

        startPos = transform.position;

        targetPos = target.position;

        currentArc = arcHeight * arcToggle;
        arcToggle *= -1;

        t = 0f;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        t += Time.deltaTime * (currentSpeed / 10f);
        t = Mathf.Clamp01(t);

        Vector2 forward = (targetPos - startPos).normalized;
        Vector2 right = new Vector2(-forward.y, forward.x);

        Vector2 controlPoint =
            (startPos + targetPos) * 0.5f + right * -currentArc;

        Vector2 a = Vector2.Lerp(startPos, controlPoint, t);
        Vector2 b = Vector2.Lerp(controlPoint, targetPos, t);
        Vector2 pos = Vector2.Lerp(a, b, t);

        transform.position = pos;

        if (t >= 1f)
            Destroy(gameObject);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        IDamageable dmg = collision.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.OnDamage(damage);
        }

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector2 knockDir = (collision.transform.position - transform.position).normalized;
            player.ApplyKnockback(knockDir * knockback);
        }

        Destroy(gameObject);
    }
}