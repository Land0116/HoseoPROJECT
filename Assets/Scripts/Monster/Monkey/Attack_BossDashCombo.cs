
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

    [Header("프리팹")]
    [SerializeField] private GameObject finalAttackPrefab;
    [SerializeField] private GameObject dashTelegraphPrefab;
    [SerializeField] private float telegraphTime = 0.3f;

    private bool hasShownAlert = false;
    private float timer;
    private bool isAttacking = false;

    private GameObject telegraphObj;
    private GameObject finalAttackObj;

    public float attackAnimationTest = 0.5f;

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsHit()) return;
        if (isAttacking) return;

        timer += Time.deltaTime;

        Transform player = monster.Player;
        if (player == null) return;

        float dist = Vector2.Distance(monster.transform.position, player.position);

        if (dist <= attackRange)
        {
            if (!hasShownAlert)
            {
                monster.ShowAttackAlert(0.5f);
                hasShownAlert = true;
            }
        }
        else hasShownAlert = false;

        if (dist <= attackRange && timer >= cooldown)
        {
            timer = 0f;
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        monster.ignoreHitAnimation = true;  
        monster.isAttacking = true;

        isAttacking = true;
        blockMovement = true;

        Transform player = monster.Player;

        monster.ForceLookAt(player.position);
        yield return Dash(player, dash1Damage);
        yield return new WaitForSeconds(firstDashDelay);

        monster.ForceLookAt(player.position);
        yield return Dash(player, dash2Damage);
        yield return new WaitForSeconds(secondDashDelay);

        Vector2 dir = (player.position - monster.transform.position).normalized;
        Vector3 fixedPos = monster.transform.position + (Vector3)dir * 2f;

        monster.lockBossFinalAttack = true;
        monster.PlayBossFinalAttackAnimation(dir);

        yield return new WaitForSeconds(finalAttackDelay);

        monster.lockBossFinalAttack = false;

        GameObject obj = Instantiate(finalAttackPrefab, fixedPos, Quaternion.identity);

        BossAttackHitbox hitbox = obj.GetComponent<BossAttackHitbox>();
        if (hitbox != null)
            hitbox.SetDamage(finalAttackDamage);

        yield return new WaitForSeconds(attackAnimationTest);

        monster.ignoreHitAnimation = false;
        monster.isAttacking = false;
        blockMovement = false;
        isAttacking = false;
    }

    private IEnumerator Dash(Transform player, float damage)
    {
        Vector2 startPos = monster.RB.position;
        Vector2 targetPos = player.position;

        Vector2 dir = (targetPos - startPos).normalized;
        float dashDistance = Vector2.Distance(startPos, targetPos) + dashExtraDistance;
        Vector2 dashTarget = targetPos + dir * dashExtraDistance;

        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(
                dashTelegraphPrefab,
                monster.transform.position,
                Quaternion.identity
            );

            DashTelegraph telegraph = telegraphObj.GetComponent<DashTelegraph>();
            if (telegraph != null)
                telegraph.Init(dir, dashDistance, telegraphTime);
        }

        yield return new WaitForSeconds(telegraphTime);

        DestroyTelegraph();

        monster.PlayBossDashAnimation(dir);

        GameObject hitboxObj = new GameObject("DashHitbox");
        CircleCollider2D col = hitboxObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;

        BossAttackHitbox hitbox = hitboxObj.AddComponent<BossAttackHitbox>();
        hitbox.SetDamage(damage);

        hitboxObj.transform.SetParent(monster.transform);
        hitboxObj.transform.localPosition = Vector3.zero;

        Destroy(hitboxObj, 0.5f);

        while (true)
        {
            Vector2 nextPos = monster.RB.position + dir * dashSpeed * Time.fixedDeltaTime;
            monster.RB.MovePosition(nextPos);

            if (Vector2.Distance(nextPos, dashTarget) <= 0.1f) break;

            if (Vector2.Dot(dashTarget - monster.RB.position, dir) <= 0f) break;

            yield return new WaitForFixedUpdate();
        }
    }


    private void DestroyTelegraph()
    {
        if (telegraphObj != null)
        {
            Destroy(telegraphObj);
            telegraphObj = null;
        }
    }
}