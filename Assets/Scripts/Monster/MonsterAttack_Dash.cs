using System.Runtime.CompilerServices;
using UnityEngine;

public class MonsterAttack_Dash : AttackPattern
{
    [Header("공격 범위")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float chargeTime = 2f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashExtraDistance = 1f;

    [Header("Damage")]
    [SerializeField] private float dashdamage = 20f;

    [SerializeField] private GameObject dashTelegraphPrefab;
    private DashTelegraph telegraphInstance;

    private float timer;
    private Vector2 dashDirection;
    private Vector2 dashTargetPosition;
    private Vector2 targetPosition;
    private bool alertTriggered = false;
    private enum State
    {
        Idle, Charge, Dash
    }

    private State state = State.Idle;

    private bool wasInRange = false;

    private void Awake()
    {
        blockMovement = false;
    }

    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;

        float distance = Vector2.Distance(monster.transform.position, monster.Player.position);
        bool isInRange = distance <= detectRange;

        switch (state)
        {
            case State.Idle:

                blockMovement = false;
                timer += Time.deltaTime;

                if (isInRange && !wasInRange)
                {
                    monster.ShowAttackAlert(0.3f);
                    alertTriggered = true;
                }

                if (timer >= dashCooldown && isInRange)
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
                    DestroyTelegraph();
                    break;
                }

                float baseDistance = Vector2.Distance(monster.transform.position, targetPosition);
                float dashLength = baseDistance + dashExtraDistance;

                dashDirection = (targetPosition - (Vector2)monster.transform.position).normalized;
                dashTargetPosition = (Vector2)monster.transform.position + dashDirection * dashLength;

                if (telegraphInstance == null && dashTelegraphPrefab != null)
                {
                    GameObject obj = Instantiate(
                        dashTelegraphPrefab,
                        monster.transform.position,
                        Quaternion.identity
                    );

                    telegraphInstance = obj.GetComponent<DashTelegraph>();
                    telegraphInstance.Init(dashDirection, dashLength, chargeTime);
                }

                if (timer >= chargeTime)
                {
                    timer = 0f;
                    state = State.Dash;
                }

                break;

            case State.Dash:

                blockMovement = true;

                Vector2 nextPos =
                    monster.RB.position + dashDirection * dashSpeed * Time.fixedDeltaTime;

                monster.RB.MovePosition(nextPos);

                if (Vector2.Distance(nextPos, dashTargetPosition) <= 0.1f ||
                    Vector2.Dot(dashTargetPosition - monster.RB.position, dashDirection) <= 0f)
                {
                    state = State.Idle;
                    timer = 0f;
                    DestroyTelegraph();
                }

                break;
        }

        wasInRange = isInRange;
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
                DestroyTelegraph();
            }
        }

        state = State.Idle;
        timer = 0f;
        DestroyTelegraph();
    }
    private void DestroyTelegraph()
    {
        if (telegraphInstance != null)
        {
            GameObject.Destroy(telegraphInstance.gameObject);
            telegraphInstance = null;
        }
    }
}