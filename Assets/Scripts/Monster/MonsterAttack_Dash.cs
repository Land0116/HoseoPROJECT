using System.Runtime.CompilerServices;
using UnityEngine;

public class MonsterAttack_Dash: AttackPattern
{
    [Header("���� ����")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float chargeTime = 2f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashExtraDistance = 1f;

    private float timer;
    private Vector2 dashDirection;

    private Vector2 dashTargetPosition;
    
    [SerializeField] private float dashdamage = 20f;
    private Vector2 targetPosition;
    private enum State
    {
        Idle, Charge, Dash
    }

    private State state = State.Idle;

    private void Awake()
    {
        blockMovement = false;
    }

    public override void Execute()
    {
        if (monster == null) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);

        switch (state)
        {
            case State.Idle:

                blockMovement = false;
                timer += Time.deltaTime;


                if (timer >= dashCooldown  && distance <= detectRange)
                {
                    timer = 0f;
                    targetPosition = monster.Player.position;
                    blockMovement = true;
                    state = State.Charge;
                }
                break;

            case State.Charge:
                blockMovement = true;
                timer += Time.deltaTime;

                if (distance > detectRange)
                {
                    state = State.Idle;
                    timer = 0f;
                    return;
                }

                if (timer >= chargeTime)
                {
                    
                    dashDirection =
                        (targetPosition - (Vector2)monster.transform.position).normalized;

                    dashTargetPosition = targetPosition + dashDirection * dashExtraDistance;

                    

                    timer = 0f;
                    state = State.Dash;
                }
                break;

            case State.Dash:
                blockMovement = true;

                Vector2 nextPos = monster.RB.position + dashDirection * dashSpeed * Time.fixedDeltaTime;
                monster.RB.MovePosition(nextPos);

                // 1. 가까워지면 멈춤
                if (Vector2.Distance(nextPos, dashTargetPosition) <= 0.1f)
                {
                    
                    state = State.Idle;
                    timer = 0f;
                    break;
                }

                // 2. 지나쳤으면 멈춤 
                Vector2 toTarget = dashTargetPosition - monster.RB.position;
                if (Vector2.Dot(toTarget, dashDirection) <= 0f)
                {
                    
                    state = State.Idle;
                    timer = 0f;
                }

                break;
        }
    }



        private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Dash) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.OnDamage(dashdamage);
            }
        }
        state = State.Idle;
    }
}

