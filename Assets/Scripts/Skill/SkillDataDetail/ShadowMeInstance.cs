using UnityEngine;
using System.Collections;

public class ShadowMeInstance : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private Vector2 lastLookDir = Vector2.down;
    private string currentAnim;

    private float damage;
    private float duration;

    [Header("공격 설정")]
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float range = 5f;

    [Header("기존 플레이어 총알 프리팹")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("총알 생성 위치 오프셋")]
    [SerializeField] private float bulletSpawnForwardOffset = 0.35f;

    public void Init(float dmg, float dur)
    {
        damage = dmg;
        duration = dur;

        StartCoroutine(LifeCycle());
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            Monster target = FindNearestMonster();

            if (target == null)
            {
                PlayIdleAnimation();
                continue;
            }

            Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

            if (dir.sqrMagnitude <= 0.0001f)
                dir = lastLookDir;

            // 핵심:
            // 분신이 바라보는 방향 저장
            lastLookDir = dir;

            PlayAttackAnimation(lastLookDir);

            // 핵심:
            // 총알도 분신이 바라보는 방향으로 발사
            Shoot(lastLookDir);
        }
    }

    private void PlayIdleAnimation()
    {
        float angle = Mathf.Atan2(lastLookDir.y, lastLookDir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Idle";

        PlayAnim(anim);
    }

    private void PlayAttackAnimation(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
            lastLookDir = dir.normalized;

        float angle = Mathf.Atan2(lastLookDir.y, lastLookDir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Attack";

        PlayAnim(anim);
    }

    private void PlayAnim(string animName)
    {
        if (animator == null) return;
        if (currentAnim == animName) return;

        currentAnim = animName;
        animator.Play(animName);
    }

    private void Shoot(Vector2 shootDir)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[ShadowMeInstance] bulletPrefab이 없음");
            return;
        }

        if (shootDir.sqrMagnitude <= 0.0001f)
            shootDir = lastLookDir;

        if (shootDir.sqrMagnitude <= 0.0001f)
            shootDir = Vector2.down;

        shootDir.Normalize();

        // 분신이 바라보는 방향으로 총알 생성 위치 보정
        Vector3 spawnPosition =
            transform.position + (Vector3)(shootDir * bulletSpawnForwardOffset);

        // 핵심:
        // ButtonSpawn은 transform.right 방향으로 날아가므로
        // 총알의 right 방향이 shootDir을 바라보게 회전시킨다.
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

        GameObject bulletObject = Instantiate(
            bulletPrefab,
            spawnPosition,
            bulletRotation
        );

        InitPlayerBullet(bulletObject);
    }

    private void InitPlayerBullet(GameObject bulletObject)
    {
        if (bulletObject == null)
            return;

        ButtonSpawn bullet = bulletObject.GetComponent<ButtonSpawn>();

        if (bullet == null)
        {
            Debug.LogWarning("[ShadowMeInstance] 기존 플레이어 총알 프리팹에 ButtonSpawn이 없음");
            return;
        }

        // 분신이 쏜 총알의 데미지
        bullet.SetDamage(damage);

        // 발사자 설정.
        // 분신 콜라이더와 총알이 부딪히는 것을 막고 싶으면 gameObject 사용.
        bullet.SetOwner(gameObject);
    }
    
    private Monster FindNearestMonster()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag("Monster")) continue;

            Monster monster = hit.GetComponent<Monster>();

            if (monster == null)
                monster = hit.GetComponentInParent<Monster>();

            if (monster == null)
                continue;

            float dist = Vector2.Distance(transform.position, monster.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = monster;
            }
        }

        return nearest;
    }

    private string GetDirectionName(float angle)
    {
        if (angle >= -22.5f && angle < 22.5f)
            return "SideR";
        if (angle >= 22.5f && angle < 67.5f)
            return "QBackR";
        if (angle >= 67.5f && angle < 112.5f)
            return "Back";
        if (angle >= 112.5f && angle < 157.5f)
            return "QBackL";
        if (angle >= 157.5f || angle < -157.5f)
            return "SideL";
        if (angle >= -157.5f && angle < -112.5f)
            return "QFrontL";
        if (angle >= -112.5f && angle < -67.5f)
            return "Front";
        if (angle >= -67.5f && angle < -22.5f)
            return "QFrontR";

        return "Front";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}