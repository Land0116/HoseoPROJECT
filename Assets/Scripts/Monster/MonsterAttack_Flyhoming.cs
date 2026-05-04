using UnityEngine;

public class MonsterAttack_Flyhoming : AttackPattern
{
    [Header("������������")]
    [SerializeField] private float homingSpeed = 3f;
    [SerializeField] private float detectRange = 8f;

    [Header("������ ����")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageInterval = 1f;

    private float damageTimer = 0;
    private bool isHoming = false;

    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;
        if (monster.IsMovementLocked()) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);
        if (distance <= detectRange)
        {
            isHoming = true;
            blockMovement = true;
        }

        if (!isHoming) return;

        Vector2 direction = ((Vector2)monster.Player.position - monster.RB.position).normalized;
        Vector2 newPos = monster.RB.position + direction * monster.GetMoveSpeed() * Time.fixedDeltaTime;
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
