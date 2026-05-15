using UnityEngine;

public class RobotAttackHitbox : MonoBehaviour
{
    private float damage;
    private float knockbackForce;
    private Transform attacker;

    private bool hasHit = false;

    public void Init(float dmg, float kb, Transform attackerTransform)
    {
        damage = dmg;
        knockbackForce = kb;
        attacker = attackerTransform;

        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        if (!collision.CompareTag("Player")) return;

        hasHit = true;

        // µ¥¹ÌÁö
        IDamageable dmg = collision.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.OnDamage(damage);
        }

        // ³Ë¹é
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector2 dir = (collision.transform.position - attacker.position).normalized;
            player.ApplyKnockback(dir * knockbackForce);
        }
    }
}