//using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MonsterAttack_Homing : AttackPattern
{
    [Header("공격 스텟")]
    [SerializeField]
    private float detectRange = 6f;
    [SerializeField]
    private float homingSpeed = 4f;
    [SerializeField]
    private float damage = 6f;

    [Header("공격 주기")]
    [SerializeField]
    private float damageInterval = 0.1f;
    private float damageTimer = 0.5f;

    private bool isHoming = false;
    private bool hasAlerted = false;
    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;
        if (monster.IsMovementLocked()) return;

        if (monster.IsHit()) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);

        if (distance <= detectRange)
        {
            isHoming = true;
            blockMovement = true;
            monster.isAttacking = true;

            if (!hasAlerted)
            {
                monster.ShowAttackAlert(0.3f);
                hasAlerted = true;
            }
        }
        else
        {
            isHoming = false;
            blockMovement = false;
            monster.isAttacking = false;

            hasAlerted = false;
        }
        if (!isHoming) return;

        Vector2 direction = ((Vector2)monster.Player.position - monster.RB.position).normalized;

        monster.PlayAttackAnimation(direction);//*

        Vector2 newPos = monster.RB.position + direction * (homingSpeed * monster.GetSpeedRatio()) * Time.fixedDeltaTime;
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
