using System.Collections;
using UnityEngine;

public class PrincessDollAttack : AttackPattern
{
    [SerializeField] private GameObject[] explosionEffectPrefabs;

    [Header("Detect")]
    [SerializeField] private float detectRange = 6f;

    [Header("Dash Attack")]
    [SerializeField] private int dashMinCount = 3;
    [SerializeField] private int dashMaxCount = 5;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashPauseTime = 0.25f;
    [SerializeField] private float dashPatternCooldown = 2f;
    [SerializeField] private float dashDamage = 1f;
    [SerializeField] private float dashDuration = 0.35f;

    private float dashTimer;
    private float dashPauseTimer;
    private float patternCooldownTimer;

    private bool dashHit;
    private bool isDashing;
    private bool isTelegraphing = false;

    [Header("8 Direction Attack")]
    [SerializeField] private GameObject eightDirPrefab;
    [SerializeField] private float eightFadeTime = 0.7f;
    [SerializeField] private float eightDamage = 1f;
    [SerializeField] private float eightPatternCooldown = 2f;

    [Header("8 Direction Unit Size")]
    [SerializeField] private Vector3 eightDirScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float eightHitRadiusMultiplier = 0.5f;

    [Header("8 Direction Spawn Origin")]
    [SerializeField] private Transform eightOrigin;
    [SerializeField] private float eightDistance = 1f;

    [Header("Explosion Attack")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionRange = 4f;
    [SerializeField] private int explosionCount = 4;
    [SerializeField] private float explosionFadeTime = 0.7f;
    [SerializeField] private float explosionDamage = 1f;
    [SerializeField] private float explosionPatternCooldown = 3f;
    [SerializeField] private float stuntime = 3f;

    [Header("Explosion Unit Size")]
    [SerializeField] private float explosionUnitRadius = 1.5f;

    [SerializeField] private GameObject dashTelegraphPrefab;

    private enum State
    {
        Idle,
        Dash,
        DashPause,
        Eight,
        Explosion,
        Cooldown
    }

    private State state = State.Idle;

    private int dashCount;
    private int dashIndex;

    private Vector2 dashDir;
    private Vector2 dashTarget;

    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;

        float dist = Vector2.Distance(monster.transform.position, monster.Player.position);

        switch (state)
        {
            case State.Idle:
                blockMovement = false;
                isDashing = false;

                patternCooldownTimer -= Time.deltaTime;

                if (dist <= detectRange && patternCooldownTimer <= 0f)
                {
                    dashCount = Random.Range(dashMinCount, dashMaxCount + 1);
                    dashIndex = 0;
                    dashTimer = 0f;
                    dashPauseTimer = 0f;

                    state = State.Dash;
                }
                break;

            case State.Dash:
                blockMovement = true;

                if (dashTimer <= 0f && !isTelegraphing)
                {
                    dashTarget = monster.Player.position;
                    dashDir = ((Vector2)dashTarget - (Vector2)monster.transform.position).normalized;
                    dashHit = false;

                    float dashDistance = Vector2.Distance(monster.transform.position, dashTarget);

                    StartCoroutine(DashTelegraphRoutine(dashDir, dashDistance));
                    return;
                }

                if (isTelegraphing)
                    return;

                isDashing = true;

                dashTimer += Time.fixedDeltaTime;

                Vector2 nextPos = monster.RB.position + dashDir * dashSpeed * Time.fixedDeltaTime;
                monster.RB.MovePosition(nextPos);

                if (dashTimer >= dashDuration)
                {
                    dashTimer = 0f;
                    isDashing = false;
                    state = State.DashPause;
                }
                break;

            case State.DashPause:
                blockMovement = true;
                isDashing = false;

                dashPauseTimer += Time.deltaTime;

                if (dashPauseTimer >= dashPauseTime)
                {
                    dashPauseTimer = 0f;
                    dashIndex++;

                    if (dashIndex >= dashCount)
                        state = State.Eight;
                    else
                        state = State.Dash;
                }
                break;

            case State.Eight:
                blockMovement = true;

                if (!isDashing)
                {
                    isDashing = true;
                    StartCoroutine(EightDirectionAttack());
                }
                break;

            case State.Cooldown:
                blockMovement = false;

                patternCooldownTimer += Time.deltaTime;

                if (patternCooldownTimer >= dashPatternCooldown)
                {
                    patternCooldownTimer = 0f;
                    state = State.Explosion;
                }
                break;

            case State.Explosion:
                blockMovement = true;

                StartCoroutine(ExplosionAttack());

                patternCooldownTimer = explosionPatternCooldown;
                state = State.Idle;
                break;
        }
    }

    private IEnumerator DashTelegraphRoutine(Vector2 dir, float distance)
    {
        isTelegraphing = true;

        GameObject teleObj = null;

        if (dashTelegraphPrefab != null)
        {
            teleObj = Instantiate(
                dashTelegraphPrefab,
                monster.transform.position,
                Quaternion.identity
            );

            DashTelegraph tele = teleObj.GetComponent<DashTelegraph>();
            if (tele != null)
            {
                tele.Init(dir, distance, dashPauseTime);
            }
        }

        yield return new WaitForSeconds(dashPauseTime);

        if (teleObj != null)
            Destroy(teleObj);

        isTelegraphing = false;

        dashTimer = 0.0001f;
        isDashing = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;
        if (dashHit) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.OnDamage(dashDamage);
            }

            dashHit = true;
        }
    }

    private IEnumerator EightDirectionAttack()
    {
        Vector2 center = monster.transform.position;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 pos = center + dir * eightDistance;

            float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            GameObject obj = Instantiate(
                eightDirPrefab,
                pos,
                Quaternion.Euler(0, 0, rotZ)
            );

            obj.transform.localScale = eightDirScale;

            var hitbox = obj.GetComponent<EightDirectionHitbox>();
            if (hitbox != null)
            {
                hitbox.SetDamage(eightDamage);
            }
        }

        yield return new WaitForSeconds(eightPatternCooldown);

        isDashing = false;
        patternCooldownTimer = 0f;
        state = State.Cooldown;
    }

    private IEnumerator ExplosionAttack()
    {
        Vector2 center = monster.transform.position;

        SpawnExplosion(center);

        for (int i = 0; i < explosionCount; i++)
        {
            Vector2 randomPos = center + Random.insideUnitCircle * explosionRange;
            SpawnExplosion(randomPos);
        }

        yield return new WaitForSeconds(explosionPatternCooldown);
    }

    private void SpawnExplosion(Vector2 pos)
    {
        GameObject obj = Instantiate(explosionPrefab, pos, Quaternion.identity);

        obj.transform.localScale = Vector3.one * explosionUnitRadius * 2f;

        StartCoroutine(ExplosionDamage(obj.transform));
    }

    private IEnumerator ExplosionDamage(Transform obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        float t = 0f;

        while (t < explosionFadeTime)
        {
            t += Time.deltaTime;
            float alpha = (t / explosionFadeTime) * 0.65f;

            if (sr != null)
                sr.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        if (explosionEffectPrefabs != null && explosionEffectPrefabs.Length > 0)
        {
            int index = Random.Range(0, explosionEffectPrefabs.Length);

            GameObject effect = Instantiate(
                explosionEffectPrefabs[index],
                obj.position,
                Quaternion.identity
            );

            effect.transform.localScale = obj.localScale;
        }

        if (monster.Player != null)
        {
            float dist = Vector2.Distance(obj.position, monster.Player.position);

            if (dist <= explosionUnitRadius &&
                monster.Player.TryGetComponent<PlayerController>(out var player))
            {
                player.OnDamage(explosionDamage);
                player.ApplyExplosionStun(stuntime);
            }
        }

        Destroy(obj.gameObject);
    }
}