using UnityEngine;

public class MonsterAttack_Flyhoming : AttackPattern
{
    [Header("Speed, Detech")]
    [SerializeField] private float homingSpeed = 3f;
    [SerializeField] private float detectRange = 8f;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageInterval = 1f;

    private float damageTimer = 0;
    private bool isHoming = false;
    private bool alertShown = false;
    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;
        if (monster.IsMovementLocked()) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);
        if (distance <= detectRange)
        {
            if (!alertShown)
            {
                monster.ShowAttackAlert(0.5f);
                alertShown = true;
            }

            isHoming = true;
            blockMovement = true;
        }
        else
        {
            isHoming = false;
            blockMovement = false;
            alertShown = false;
        }

        if (!isHoming) return;

        Vector2 direction = ((Vector2)monster.Player.position - monster.RB.position).normalized;
        float speed = monster.GetMoveSpeed() * homingSpeed;

        Vector2 newPos = monster.RB.position + direction * speed * Time.fixedDeltaTime;
        monster.RB.MovePosition(newPos);


    }

    private void OnCollisionStay2D(Collision2D collision)
    {

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
