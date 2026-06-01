/*using System.Collections;
using UnityEngine;

public class RobotAttackPattern : AttackPattern
{
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip bulletSound;

    [Header("Ranged Move Setting")]
    [SerializeField] private float rangedMoveSpeed = 4f;
    [SerializeField] private float moveRange = 4f;

    private Vector2 moveTarget;
    private bool hasMoveTarget = false;

    [Header("AttackRange")]
    [SerializeField] private float meleeRange = 7f;
    [SerializeField] private float rangedMinRange = 7f;
    [SerializeField] private float rangedMaxRange = 15f;

    [Header("All")]
    [SerializeField] private float cooldown = 0.5f;

    [Header("Close Attack")]
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private float meleeDamage = 1f;
    [SerializeField] private float meleeKnockback = 8f;
    [SerializeField] private float attack1Delay = 0.5f;
    [SerializeField] private float attackDistance = 3;

    [Header("Misile Attack")]
    [SerializeField] private float missilePrepareTime = 0.3f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletCount = 6;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float rangedKnockback = 6f;
    [SerializeField] private float minFireDelay = 0.2f;
    [SerializeField] private float maxFireDelay = 0.6f;
    [SerializeField] private float bulletSpeed = 8f;
    private int fireIndex = 0;
    private float timer;
    private bool isAttacking = false;
    private bool hasShownAlert = false;
    [SerializeField] private GameObject dashTelegraphPrefab;
    [SerializeField] private float telegraphTime = 0.5f;
    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsHit()) return;

        Transform player = monster.Player;
        if (player == null) return;

        float dist = Vector2.Distance(monster.transform.position, player.position);
        
        // 경고 UI
        if (dist <= rangedMaxRange)
        {
            if (!hasShownAlert)
            {
                monster.ShowAttackAlert(0.5f);
                hasShownAlert = true;
            }
        }
        else
        {
            hasShownAlert = false;
        }

        timer += Time.deltaTime;
        if (timer < cooldown) return;

        // 근접
        if (dist <= meleeRange && !isAttacking)
        {
            StartCoroutine(MeleeAttack(player));
        }
        // 원거리
        else if (dist >= rangedMinRange && dist <= rangedMaxRange && !isAttacking)
        {
            StartCoroutine(RangedAttack(player));
        }

        // 핵심: 공격 중에도 계속 이동
        if (isAttacking && dist >= rangedMinRange)
        {
            MoveWhileAttacking();
        }
    }

    /*private IEnumerator MeleeAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = true;
        timer = 0f;

        Vector2 dir = (player.position - monster.transform.position).normalized;

        Vector3 fixedPos = monster.transform.position + (Vector3)dir * attackDistance;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject telegraphObj = null;
        SpriteRenderer sr = null;

        // ===== 전조 생성 =====
        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(dashTelegraphPrefab, fixedPos, rot);
            telegraphObj.transform.localScale = meleePrefab.transform.localScale;

            sr = telegraphObj.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        float t = 0f;

        // ===== 0 → 0.8 알파까지 증가 =====
        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float alpha = t / telegraphTime;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 0.8f, alpha);
                sr.color = c;
            }

            yield return null;
        }

        // ===== 전조 제거 =====
        if (telegraphObj != null)
            Destroy(telegraphObj);

        // ===== 공격 실행 =====
        GameObject obj = Instantiate(meleePrefab, fixedPos, rot);

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound, 0.7f);
        }

        monster.PlayAttackAnimation(dir);

        RobotAttackHitbox hitbox = obj.GetComponent<RobotAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(meleeDamage, meleeKnockback, monster.transform);
        }

        Destroy(obj, 0.5f);

        yield return new WaitForSeconds(0.3f);

        monster.SetAttackAnimationLock(false);

        blockMovement = false;
        isAttacking = false;
    }
    private IEnumerator MeleeAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = true;
        timer = 0f;

        // ===== 초기 방향 & 위치 계산 (전조용) =====
        Vector2 dir = (player.position - monster.transform.position).normalized;

        float dist = Vector2.Distance(monster.transform.position, player.position);
        float finalDistance = Mathf.Min(dist, attackDistance);

        Vector3 fixedPos = monster.transform.position + (Vector3)dir * finalDistance;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject telegraphObj = null;
        SpriteRenderer sr = null;

        // ===== 전조 생성 =====
        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(dashTelegraphPrefab, fixedPos, rot);
            telegraphObj.transform.localScale = meleePrefab.transform.localScale;

            sr = telegraphObj.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        float t = 0f;

        // ===== 전조 알파 증가 (0 → 0.8) =====
        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float alpha = t / telegraphTime;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 0.8f, alpha);
                sr.color = c;
            }

            yield return null;
        }

        // ===== 전조 제거 =====
        if (telegraphObj != null)
            Destroy(telegraphObj);

        // ===== 공격 직전 다시 계산 (플레이어 움직임 반영) =====
        dir = (player.position - monster.transform.position).normalized;

        dist = Vector2.Distance(monster.transform.position, player.position);
        finalDistance = Mathf.Min(dist, attackDistance);

        fixedPos = monster.transform.position + (Vector3)dir * finalDistance;
        rot = Quaternion.FromToRotation(Vector3.right, dir);

        // ===== 실제 공격 생성 =====
        GameObject obj = Instantiate(meleePrefab, fixedPos, rot);

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound, 0.7f);
        }

        monster.PlayAttackAnimation(dir);

        RobotAttackHitbox hitbox = obj.GetComponent<RobotAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(meleeDamage, meleeKnockback, monster.transform);
        }

        Destroy(obj, 0.5f);

        yield return new WaitForSeconds(0.3f);

        monster.SetAttackAnimationLock(false);

        blockMovement = false;
        isAttacking = false;
    }
    private IEnumerator RangedAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = false;
        timer = 0f;

        for (int i = 0; i < bulletCount; i++)
        {
            if (player == null) break;

            float dist = Vector2.Distance(monster.transform.position, player.position);

            if (dist <= meleeRange)
            {
                isAttacking = false;
                monster.RB.linearVelocity = Vector2.zero;

                StartCoroutine(MeleeAttack(player));
                yield break;
            }

            // 방향 먼저 고정
            Vector2 dir = (player.position - monster.transform.position).normalized;

            // 준비 애니메이션
            monster.PlayMissilePrepareAnimation(dir);
            yield return new WaitForSeconds(missilePrepareTime);

            // 발사 애니메이션
            monster.PlayMissileAnimation(dir);

            GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity);
            if (audioSource != null && bulletSound != null) //*
            {
                audioSource.PlayOneShot(bulletSound);
            }

            RobotBullet rb = bullet.GetComponent<RobotBullet>();

            if (rb != null)
            {
                rb.Init(dir, bulletDamage, rangedKnockback, bulletSpeed, player);
            }

            MoveWhileAttacking();

            float delay = Random.Range(minFireDelay, maxFireDelay);
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(0.2f);

        monster.SetAttackAnimationLock(false);//* 0601

        isAttacking = false;
    }


    private Vector2 GetRandomMovePoint()
    {
        Vector2 center = monster.transform.position;

        float x = Random.Range(-moveRange, moveRange);
        float y = Random.Range(-moveRange, moveRange);

        return center + new Vector2(x, y);
    }

    private void MoveWhileAttacking()
    {
        if (!hasMoveTarget || Vector2.Distance(monster.RB.position, moveTarget) < 0.5f)
        {
            moveTarget = GetRandomMovePoint();
            hasMoveTarget = true;
        }

        Vector2 dir = (moveTarget - monster.RB.position).normalized;
        monster.RB.linearVelocity = dir * rangedMoveSpeed;
    }
}*/
using System.Collections;
using UnityEngine;

public class RobotAttackPattern : AttackPattern
{
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip bulletSound;

    [Header("Ranged Move Setting")]
    [SerializeField] private float rangedMoveSpeed = 4f;
    [SerializeField] private float moveRange = 4f;

    private Vector2 moveTarget;
    private bool hasMoveTarget = false;

    [Header("AttackRange")]
    [SerializeField] private float meleeRange = 7f;
    [SerializeField] private float rangedMinRange = 7f;
    [SerializeField] private float rangedMaxRange = 15f;

    [Header("All")]
    [SerializeField] private float cooldown = 0.5f;

    [Header("Close Attack")]
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private float meleeDamage = 1f;
    [SerializeField] private float meleeKnockback = 8f;
    [SerializeField] private float attack1Delay = 0.5f;
    [SerializeField] private float attackDistance = 3;

    [Header("Misile Attack")]
    [SerializeField] private float missilePrepareTime = 0.3f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletCount = 6;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float rangedKnockback = 6f;
    [SerializeField] private float minFireDelay = 0.2f;
    [SerializeField] private float maxFireDelay = 0.6f;
    [SerializeField] private float bulletSpeed = 8f;

    private int fireIndex = 0;
    private float timer;
    private bool isAttacking = false;
    private bool hasShownAlert = false;

    [SerializeField] private GameObject dashTelegraphPrefab;
    [SerializeField] private float telegraphTime = 0.5f;

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsHit()) return;

        Transform player = monster.Player;
        if (player == null) return;

        float dist = Vector2.Distance(monster.transform.position, player.position);

        if (dist <= rangedMaxRange)
        {
            if (!hasShownAlert)
            {
                monster.ShowAttackAlert(0.5f);
                hasShownAlert = true;
            }
        }
        else
        {
            hasShownAlert = false;
        }

        timer += Time.deltaTime;
        if (timer < cooldown) return;

        if (dist <= meleeRange && !isAttacking)
        {
            StartCoroutine(MeleeAttack(player));
        }
        else if (dist >= rangedMinRange && dist <= rangedMaxRange && !isAttacking)
        {
            StartCoroutine(RangedAttack(player));
        }

        if (isAttacking && dist >= rangedMinRange)
        {
            MoveWhileAttacking();
        }
    }

    /*private IEnumerator MeleeAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = true;
        timer = 0f;

        Vector2 dir = (player.position - monster.transform.position).normalized;

        float dist = Vector2.Distance(monster.transform.position, player.position);
        float finalDistance = Mathf.Min(dist, attackDistance);

        Vector3 fixedPos = monster.transform.position + (Vector3)dir * finalDistance;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject telegraphObj = null;
        SpriteRenderer sr = null;

        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(dashTelegraphPrefab, fixedPos, rot);
            telegraphObj.transform.localScale = meleePrefab.transform.localScale;

            sr = telegraphObj.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        float t = 0f;

        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float alpha = t / telegraphTime;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 0.8f, alpha);
                sr.color = c;
            }

            yield return null;
        }

        if (telegraphObj != null)
            Destroy(telegraphObj);

        dir = (player.position - monster.transform.position).normalized;

        dist = Vector2.Distance(monster.transform.position, player.position);
        finalDistance = Mathf.Min(dist, attackDistance);

        fixedPos = monster.transform.position + (Vector3)dir * finalDistance;
        rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject obj = Instantiate(meleePrefab, fixedPos, rot);

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound, 0.7f);
        }

        monster.PlayAttackAnimation(dir);

        RobotAttackHitbox hitbox = obj.GetComponent<RobotAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(meleeDamage, meleeKnockback, monster.transform);
        }

        Destroy(obj, 0.5f);

        yield return new WaitForSeconds(0.3f);

        monster.SetAttackAnimationLock(false);

        blockMovement = false;
        isAttacking = false;
    }*/
    private IEnumerator MeleeAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = true;
        timer = 0f;

        Vector2 dir = (player.position - monster.transform.position).normalized;

        float dist = Vector2.Distance(monster.transform.position, player.position);
        float finalDistance = Mathf.Min(dist, attackDistance);

        Vector3 fixedPos = monster.transform.position + (Vector3)dir * finalDistance;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject telegraphObj = null;
        SpriteRenderer sr = null;

        // 전조 생성 (여기 위치 = 최종 공격 위치)
        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(dashTelegraphPrefab, fixedPos, rot);
            telegraphObj.transform.localScale = meleePrefab.transform.localScale;

            sr = telegraphObj.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        float t = 0f;

        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float alpha = t / telegraphTime;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 0.8f, alpha);
                sr.color = c;
            }

            yield return null;
        }

        // 전조 삭제
        if (telegraphObj != null)
            Destroy(telegraphObj);


        GameObject obj = Instantiate(meleePrefab, fixedPos, rot);

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound, 0.7f);
        }

        monster.PlayAttackAnimation(dir);

        RobotAttackHitbox hitbox = obj.GetComponent<RobotAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(meleeDamage, meleeKnockback, monster.transform);
        }

        Destroy(obj, 0.5f);

        yield return new WaitForSeconds(0.3f);

        monster.SetAttackAnimationLock(false);

        blockMovement = false;
        isAttacking = false;
    }

    private IEnumerator RangedAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = false;
        timer = 0f;

        for (int i = 0; i < bulletCount; i++)
        {
            if (player == null) break;

            float dist = Vector2.Distance(monster.transform.position, player.position);

            if (dist <= meleeRange)
            {
                isAttacking = false;
                monster.RB.linearVelocity = Vector2.zero;

                StartCoroutine(MeleeAttack(player));
                yield break;
            }

            Vector2 dir = (player.position - monster.transform.position).normalized;

            monster.PlayMissilePrepareAnimation(dir);
            yield return new WaitForSeconds(missilePrepareTime);

            monster.PlayMissileAnimation(dir);

            GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity);

            if (audioSource != null && bulletSound != null)
            {
                audioSource.PlayOneShot(bulletSound);
            }

            RobotBullet rb = bullet.GetComponent<RobotBullet>();

            if (rb != null)
            {
                rb.Init(dir, bulletDamage, rangedKnockback, bulletSpeed, player);
            }

            MoveWhileAttacking();

            float delay = Random.Range(minFireDelay, maxFireDelay);
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(0.2f);

        monster.SetAttackAnimationLock(false);

        isAttacking = false;
    }

    private Vector2 GetRandomMovePoint()
    {
        Vector2 center = monster.transform.position;

        float x = Random.Range(-moveRange, moveRange);
        float y = Random.Range(-moveRange, moveRange);

        return center + new Vector2(x, y);
    }

    private void MoveWhileAttacking()
    {
        if (!hasMoveTarget || Vector2.Distance(monster.RB.position, moveTarget) < 0.5f)
        {
            moveTarget = GetRandomMovePoint();
            hasMoveTarget = true;
        }

        Vector2 dir = (moveTarget - monster.RB.position).normalized;
        monster.RB.linearVelocity = dir * rangedMoveSpeed;
    }
}