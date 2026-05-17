using UnityEngine;

public class MonsterAttack_DashKnockback : AttackPattern
{
    [Header("Dash Settings")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float chargeTime = 1.5f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashExtraDistance = 1f;

    [Header("Damage & Knockback")]
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private float knockbackForce = 8f;

    private bool wasInRange = false;

    private float timer;
    private Vector2 dashDirection;
    private Vector2 dashTargetPosition;
    private Vector2 targetPosition;

    [SerializeField] private GameObject dashTelegraphPrefab;
    private DashTelegraph telegraphInstance;

    private enum State
    {
        Idle,
        Charge,
        Dash
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
        bool isInRange = distance <= detectRange;
        if (isInRange && !wasInRange)
        {
            monster.ShowAttackAlert(0.3f);
        }
        wasInRange = isInRange;

        switch (state)
        {
            case State.Idle:
                blockMovement = false;
                timer += Time.deltaTime;
                if (timer >= dashCooldown && distance <= detectRange)
                {
                    timer = 0f;
                    targetPosition = monster.Player.position;

                    Vector2 dir = (targetPosition - (Vector2)monster.transform.position).normalized;

                    float dashDistance = Vector2.Distance(monster.transform.position, targetPosition) + dashExtraDistance;

                    GameObject obj = Instantiate(dashTelegraphPrefab, monster.transform.position, Quaternion.identity);
                    telegraphInstance = obj.GetComponent<DashTelegraph>();

                    telegraphInstance.Init(dir, dashDistance, chargeTime);

                    blockMovement = true;
                    state = State.Charge;
                }
                break;

            case State.Charge:
                blockMovement = true;
                timer += Time.deltaTime;

                if (distance > detectRange)
                {
                    if (telegraphInstance != null)
                    {
                        Destroy(telegraphInstance.gameObject); //*
                    }

                    state = State.Idle;
                    timer = 0f;
                    return;
                }
                if (timer >= chargeTime)
                {
                    if (telegraphInstance != null)
                    {
                        Destroy(telegraphInstance.gameObject);
                    }

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

                if (Vector2.Distance(nextPos, dashTargetPosition) <= 0.1f)
                {
                    state = State.Idle;
                    timer = 0f;
                    break;
                }

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
            // µ¥¹ÌÁö
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.OnDamage(dashDamage);
            }

            //³Ë¹é
            Monster playerMonster = collision.gameObject.GetComponent<Monster>();
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            Vector2 knockDir = ((Vector2)collision.transform.position - monster.RB.position).normalized;

            if (player != null)
            {
                player.ApplyKnockback(knockDir * knockbackForce);
            }
        }

        state = State.Idle;
        timer = 0f;
    }
}