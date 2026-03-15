using System.Runtime.CompilerServices;
using UnityEngine;

public class MonsterAttack_Dash: AttackPattern
{
    [Header("돌진 설정")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float chargeTime = 2f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.8f;
    [SerializeField] private float dashCooldown = 2f;
    private float timer;
    private Vector2 dashDirection;
    private float dashdamage = 20f;
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

                    timer = 0f;
                    state = State.Dash;
                }
                break;

            case State.Dash:
                blockMovement = true;
                monster.RB.MovePosition(monster.RB.position + dashDirection * dashSpeed * Time.fixedDeltaTime);

                timer += Time.deltaTime;

                if(timer >= dashDuration)
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

