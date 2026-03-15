using UnityEngine;

public class MonsterAttack_Melee : AttackPattern
{
    [Header("근접 공격 설정")]
    [SerializeField] private float detectRange = 3f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float chargeTime = 1f;

    [Header("공격 프리팹 ex칼")]
    [SerializeField] private GameObject knifePrefab;

    [SerializeField] private float attackDuration = 0.5f;

    private float timer;
    private Vector2 attackDirection;
    private enum State { Idle, Approach, Charge, Attack }

    private State state = State.Idle;

    public override void Execute()
    {
        if (monster == null) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);

        switch (state)
        {
            case State.Idle:
                blockMovement = false;
                if(distance <= detectRange)
                {
                    state = State.Approach;
                }
                break;

            case State.Approach:
                blockMovement = true;
                MoveToAttackPosition();
                if(distance <= attackRange)
                {
                    attackDirection = ((Vector2)monster.Player.position - monster.RB.position).normalized;
                    timer = 0f;
                    state = State.Charge;
                }
                break;

            case State.Charge:
                blockMovement = true;
                timer += Time.deltaTime;
                monster.RB.linearVelocity = Vector2.zero;//

                if (timer >= chargeTime)
                {
                    timer = 0f;
                    Attack();
                    state = State.Attack;
                }
                break;

            case State.Attack:
                blockMovement = true;

                monster.RB.linearVelocity = Vector2.zero;//

                timer += Time.deltaTime;
                if(timer >= attackDuration)
                {
                    state = State.Idle;
                }
                break;
        }
    }

    void MoveToAttackPosition()
    {
        Vector2 direction = ((Vector2)monster.Player.position - monster.RB.position).normalized;

        Vector2 newPos = monster.RB.position + direction * monster.MoveSpeed * Time.fixedDeltaTime;
        monster.RB.MovePosition(newPos);
    }
    void Attack()
    {
        

        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;

        Vector2 spawnPos = (Vector2)monster.transform.position + attackDirection * 0.4f;

        Instantiate(knifePrefab, spawnPos, Quaternion.Euler(0, 0, angle - 135f));
    }
}
