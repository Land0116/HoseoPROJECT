using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MonsterAttack_Homing : AttackPattern
{
    [Header("유도공격설정")]
    [SerializeField]
    private float detectRange = 6f;
    [SerializeField]
    private float homingSpeed = 4f;
    [SerializeField]
    private float damage = 6f;

    [Header("틱당 틱 설정")]
    [SerializeField]
    private float damageInterval = 0.1f;
    private float damageTimer = 0.5f;

    private bool isHoming = false;

    public override void Execute()
    {
        if (monster == null) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);

        if(distance <= detectRange)
        {
            isHoming = true;
            blockMovement = true;
        }

        if (!isHoming) return;

        Vector2 direction = ((Vector2)monster.Player.position - monster.RB.position).normalized;
        Vector2 newPos = monster.RB.position + direction * homingSpeed * Time.fixedDeltaTime;
        monster.RB.MovePosition(newPos);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isHoming) return;

        if (!collision.gameObject.CompareTag("Player")) return;

        damageTimer += Time.deltaTime;

        if(damageTimer >= damageInterval)
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

            if(damageable != null)
            {
                damageable.OnDamage(damage);
            }
            damageTimer = 0f;
        }
    }
}
