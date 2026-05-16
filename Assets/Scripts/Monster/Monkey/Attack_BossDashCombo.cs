using System.Collections;
using UnityEngine;

public class Attack_BossDashCombo : AttackPattern
{
    [Header("공격 설정")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float cooldown = 3f;

    [Header("대쉬 설정")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashExtraDistance = 1f;

    [Header("대미지")]
    [SerializeField] private float dash1Damage = 1f;
    [SerializeField] private float dash2Damage = 1f;
    [SerializeField] private float finalAttackDamage = 1f;

    [Header("단계별 딜레이")]
    [SerializeField] private float firstDashDelay = 0.5f;
    [SerializeField] private float secondDashDelay = 0.5f;
    [SerializeField] private float finalAttackDelay = 0.3f;
    [SerializeField] private float BeforefinalAttackDelay = 0.5f;

    [Header("프리팹")]
    [SerializeField] private GameObject finalAttackPrefab;

    private bool hasShownAlert = false;

    private float timer;
    private bool isAttacking = false;

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsHit()) return;
        if (isAttacking) return;

        timer += Time.deltaTime;

        Transform player = monster.Player;
        if (player == null) return;

        float dist = Vector2.Distance(monster.transform.position, player.position);

        // 범위 들어오면 즉시 느낌표
        if (dist <= attackRange)
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

        // 실제 공격 조건
        if (dist <= attackRange && timer >= cooldown)
        {
            timer = 0f;
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        blockMovement = true;

        Transform player = monster.Player;

        // 1첫번째 대쉬
        yield return Dash(player, dash1Damage);

        yield return new WaitForSeconds(firstDashDelay);

        // 2두번째 대쉬
        yield return Dash(player, dash2Damage);

        yield return new WaitForSeconds(secondDashDelay);

        // 3마지막 공격
        Vector2 dir = (player.position - monster.transform.position).normalized;

        // 공격 위치 미리 고정
        Vector3 fixedAttackPos = monster.transform.position + (Vector3)dir * 2f;

        // 방향 고정
        monster.PlayAttackAnimation(dir);

        // 딜레이 
        yield return new WaitForSeconds(finalAttackDelay);

        // 고정된 위치에 공격 생성
        GameObject obj = Instantiate(finalAttackPrefab, fixedAttackPos, Quaternion.identity);

        BossAttackHitbox hitbox = obj.GetComponent<BossAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.SetDamage(finalAttackDamage);
        }

        blockMovement = false;
        isAttacking = false;
    }

    private IEnumerator Dash(Transform player, float damage)
    {
        Vector2 startPos = monster.RB.position;
        Vector2 targetPos = player.position;

        Vector2 dir = (targetPos - startPos).normalized;
        Vector2 dashTarget = targetPos + dir * dashExtraDistance;

        monster.PlayAttackAnimation(dir);

        // 히트박스 생성
        GameObject hitboxObj = new GameObject("DashHitbox");
        CircleCollider2D col = hitboxObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;

        BossAttackHitbox hitbox = hitboxObj.AddComponent<BossAttackHitbox>();
        hitbox.SetDamage(damage);

        hitboxObj.transform.SetParent(monster.transform);
        hitboxObj.transform.localPosition = Vector3.zero;

        // 0.5초 후 자동 삭제
        Destroy(hitboxObj, 0.5f);

        while (true)
        {
            Vector2 nextPos = monster.RB.position + dir * dashSpeed * Time.fixedDeltaTime;
            monster.RB.MovePosition(nextPos);

            // 도착 체크
            if (Vector2.Distance(nextPos, dashTarget) <= 0.1f)
                break;

            // 지나침 체크
            Vector2 toTarget = dashTarget - monster.RB.position;
            if (Vector2.Dot(toTarget, dir) <= 0f)
                break;

            yield return new WaitForFixedUpdate();
        }

        yield return null;
    }
}