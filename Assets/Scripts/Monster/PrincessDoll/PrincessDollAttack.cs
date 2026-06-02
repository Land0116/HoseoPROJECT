using System.Collections;
using UnityEngine;

public class PrincessDollAttack : AttackPattern
{
    [SerializeField] private GameObject rightDashEffectPrefab;
    [SerializeField] private GameObject backDashEffectPrefab;
    [SerializeField] private GameObject leftDashEffectPrefab;
    [SerializeField] private GameObject frontDashEffectPrefab;
    private bool dashEffectSpawnedOnce;
    [Header("Dash Directional Effect Delay (Grouped)")]
    [SerializeField] private float frontBackEffectDelay = 0f;
    [SerializeField] private float leftRightEffectDelay = 0f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip eightDirSound;
    [SerializeField] private AudioClip explosionSound;

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

    [Header("Dash Effect")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private float dashEffectDelay = 0.1f;
    [SerializeField] private Vector3 dashEffectOffset;

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
    [SerializeField] private float dashEffectBeforeEndTime = 0.5f;

    [Header("8 Direction Spawn Origin")]
    [SerializeField] private Transform eightOrigin;
    [SerializeField] private float eightDistance = 1f;
    private bool dashEffectSpawned;

    [Header("8 Direction Telegraph")]
    [SerializeField] private GameObject eightTelegraphPrefab;

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
    private bool attackPlayedThisDash;

    [SerializeField] private float attackAnimLockTime = 0.4f;
    private float attackAnimLockTimer;
    private bool justEnteredDash;
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
                {
                    blockMovement = false;
                    isDashing = false;

                    patternCooldownTimer -= Time.deltaTime;

                    if (dist <= detectRange && patternCooldownTimer <= 0f)
                    {
                        dashCount = Random.Range(dashMinCount, dashMaxCount + 1);
                        dashIndex = 0;

                        attackPlayedThisDash = false;
                        attackAnimLockTimer = 0f;

                        dashTimer = 0f;
                        dashPauseTimer = 0f;

                        justEnteredDash = true;

                        state = State.Dash;
                    }
                }
                break;

            case State.Dash:
                {
                    blockMovement = true;

                    if (justEnteredDash)
                    {
                        monster.SetAttackAnimationLock(true);

                        dashTarget = monster.Player.position;
                        dashDir = ((Vector2)dashTarget - (Vector2)monster.transform.position).normalized;
                        dashHit = false;

                        float dashDistance = Vector2.Distance(monster.transform.position, dashTarget);

                        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
                        string anim = GetBossDirectionName(angle) + "_Attack";

                        monster.PlaySimpleAttackAnim(anim);

                        attackPlayedThisDash = true;
                        attackAnimLockTimer = attackAnimLockTime;

                        dashEffectSpawnedOnce = false;

                        // 여기서 바로 생성 (대쉬 시작 전)
                        SpawnDashDirectionalEffect();

                        StartCoroutine(DashTelegraphRoutine(dashDir, dashDistance));

                        justEnteredDash = false;
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

                        monster.SetAttackAnimationLock(false);

                        state = State.DashPause;
                    }
                }
                break;

            case State.DashPause:
                {
                    blockMovement = true;
                    isDashing = false;

                    dashPauseTimer += Time.deltaTime;

                    attackAnimLockTimer -= Time.deltaTime;

                    if (attackAnimLockTimer <= 0f)
                    {
                        attackPlayedThisDash = false;
                    }

                    if (dashPauseTimer >= dashPauseTime)
                    {
                        dashPauseTimer = 0f;

                        dashEffectSpawnedOnce = false;

                        dashIndex++;

                        if (dashIndex >= dashCount)
                        {
                            monster.SetAttackAnimationLock(false);
                            state = State.Eight;
                        }
                        else
                        {
                            justEnteredDash = true;
                            state = State.Dash;
                        }
                    }
                }
                break;

            case State.Eight:
                blockMovement = true;

                if (!isDashing)
                {
                    isDashing = true;

                    monster.SetAttackAnimationLock(true);

                    if (monster != null)
                    {
                        monster.SetAnimationLock(false);
                        monster.PlayForceAnimation("Attack_2", 1.8f);
                    }

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

                monster.SetAttackAnimationLock(true);

                if (monster != null)
                {
                    monster.SetAnimationLock(false);
                    monster.PlayForceAnimation("Attack_3", 1.8f);
                }

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
    private IEnumerator ReturnToIdleAfterAttack()
    {

        yield return new WaitForSeconds(0.4f);

        if (monster == null) yield break;

        Vector2 dir = monster.GetLookDirection();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = monster.GetDirectionName(angle) + "_Idle";

        monster.PlaySimpleAttackAnim(anim);
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

        GameObject[] telegraphs = new GameObject[8];

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 pos = center + dir * eightDistance;

            float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            GameObject tele = Instantiate(
                eightTelegraphPrefab,
                pos,
                Quaternion.Euler(0, 0, rotZ)
            );
            SpriteRenderer sr = tele.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(FadeTelegraph(sr, eightFadeTime));
            }
            tele.transform.localScale = eightDirScale;
            telegraphs[i] = tele;
        }

        yield return new WaitForSeconds(eightFadeTime);

        for (int i = 0; i < telegraphs.Length; i++)
        {
            if (telegraphs[i] != null)
                Destroy(telegraphs[i]);
        }

        if (audioSource != null && eightDirSound != null)
        {
            audioSource.PlayOneShot(eightDirSound);
        }
        // 2. ���� ���� ����
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

    private IEnumerator FadeTelegraph(SpriteRenderer sr, float duration)
    {
        float t = 0f;

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(0f, 0.6f, t / duration);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            yield return null;
        }
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

        StartCoroutine(ExplosionDelayRoutine(obj.transform));

        Destroy(obj, 1.5f);//* ����
    }
    private IEnumerator ExplosionDelayRoutine(Transform obj)
    {
        float delay = Random.Range(0.05f, 0.6f);

        yield return new WaitForSeconds(delay);
        
        StartCoroutine(ExplosionDamage(obj));
    }
    private IEnumerator ExplosionDamage(Transform obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        float t = 0f;

        while (t < explosionFadeTime)
        {
            t += Time.deltaTime;
            float alpha = (t / explosionFadeTime) * 0.5f;

            if (sr != null)
                sr.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        if (explosionEffectPrefabs != null && explosionEffectPrefabs.Length > 0)
        {
            int index = Random.Range(0, explosionEffectPrefabs.Length);
            if (audioSource != null && explosionSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(explosionSound, 0.5f);
                audioSource.pitch = 1f;
            }

            GameObject effect = Instantiate(
                explosionEffectPrefabs[index],
                obj.position,
                Quaternion.identity
            );

            effect.transform.localScale = obj.localScale;
            Destroy(effect, 1f); //* Effect Destroy
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
    private string GetBossDirectionName(float angle)
    {
        // SideR: 오른쪽 전체 (QBackR ~ QFrontR)
        if (angle >= -67.5f && angle < 67.5f)
            return "SideR";

        // Back: 위쪽 전체 (QBackR ~ QBackL)
        if (angle >= 67.5f && angle < 112.5f)
            return "Back";

        // SideL: 왼쪽 전체 (QBackL ~ QFrontL)
        if (angle >= 112.5f || angle < -112.5f)
            return "SideL";

        // Front: 아래쪽 전체 (QFrontL ~ QFrontR)
        return "Front";
    }
    private void SpawnDashEffectNow()
    {
        if (dashEffectPrefab != null)
        {
            Vector3 pos = monster.transform.position + dashEffectOffset;

            GameObject obj = Instantiate(dashEffectPrefab, pos, Quaternion.identity);
            if (audioSource != null && dashSound != null)
            {
                audioSource.PlayOneShot(dashSound);
            }

            Destroy(obj, 0.2f);
        }
    }
    private void SpawnDashDirectionalEffect()
    {
        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
        string dirName = GetBossDirectionName(angle);

        GameObject prefab = null;

        switch (dirName)
        {
            case "SideR":
                prefab = rightDashEffectPrefab;
                break;

            case "Back":
                prefab = backDashEffectPrefab;
                break;

            case "SideL":
                prefab = leftDashEffectPrefab;
                break;

            case "Front":
                prefab = frontDashEffectPrefab;
                break;
        }

        if (prefab != null)
        {
            Vector3 pos = monster.transform.position + dashEffectOffset;

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

            if (audioSource != null && dashSound != null)
            {
                audioSource.PlayOneShot(dashSound);
            }

            Destroy(obj, 0.2f);
        }
    }
    private IEnumerator SpawnDashEffectWithDelay()
    {
        float delay = GetDirectionalDelay();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SpawnDashDirectionalEffect();
    }
    private float GetDirectionalDelay()
    {
        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
        string dirName = GetBossDirectionName(angle);

        switch (dirName)
        {
            case "Front":
            case "Back":
                return frontBackEffectDelay;

            case "SideL":
            case "SideR":
                return leftRightEffectDelay;
        }

        return 0f;
    }
}