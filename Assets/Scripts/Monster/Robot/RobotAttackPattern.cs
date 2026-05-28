using System.Collections;
using UnityEngine;

public class RobotAttackPattern : AttackPattern
{
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
    /* public override void Execute()
     {
         if (monster == null) return;
         if (monster.IsHit()) return;
         if (isAttacking) return;

         timer += Time.deltaTime;

         Transform player = monster.Player;
         if (player == null) return;

         float dist = Vector2.Distance(monster.transform.position, player.position);

         // 범위 들어오면 ! 출력
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

         if (timer < cooldown) return;

         // 공격 시작
         if (dist <= meleeRange)
         {
             StartCoroutine(MeleeAttack(player));
         }
         else if (dist >= rangedMinRange && dist <= rangedMaxRange)
         {
             StartCoroutine(RangedAttack(player));
         }
     }*/
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

    private IEnumerator MeleeAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = true;
        timer = 0f;

        Vector2 dir = (player.position - monster.transform.position).normalized;

        Vector3 fixedPos = monster.transform.position + (Vector3)dir * attackDistance;

        GameObject telegraphObj = null;

        if (dashTelegraphPrefab != null)
        {
            telegraphObj = Instantiate(
                dashTelegraphPrefab,
                monster.transform.position,
                Quaternion.identity
            );

            DashTelegraph telegraph = telegraphObj.GetComponent<DashTelegraph>();
            if (telegraph != null)
            {
                telegraph.Init(dir, attackDistance, telegraphTime);
            }
        }

        yield return new WaitForSeconds(telegraphTime);

        if (telegraphObj != null)
            Destroy(telegraphObj);

        monster.PlayAttackAnimation(dir);

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        GameObject obj = Instantiate(meleePrefab, fixedPos, rot);

        RobotAttackHitbox hitbox = obj.GetComponent<RobotAttackHitbox>();
        if (hitbox != null)
        {
            hitbox.Init(meleeDamage, meleeKnockback, monster.transform);
        }

        Destroy(obj, 0.5f);

        yield return new WaitForSeconds(0.3f);

        blockMovement = false;
        isAttacking = false;
    }

    /*private IEnumerator RangedAttack(Transform player)
    {
        isAttacking = true;
        blockMovement = false;
        timer = 0f;

        for (int i = 0; i < bulletCount; i++) 
        { 
            if (player == null) break; 

            Vector2 dir = (player.position - monster.transform.position).normalized;
            
            monster.PlayAttackAnimation(dir); 

            GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity); 
            RobotBullet rb = bullet.GetComponent<RobotBullet>(); 
            if (rb != null) 
            {
                rb.Init(dir, bulletDamage, rangedKnockback, bulletSpeed, player);
            }

            MoveWhileAttacking();
            float delay = Random.Range(minFireDelay, maxFireDelay);
            yield return new WaitForSeconds(delay); 
        }

        yield return new WaitForSeconds(0.3f);

        blockMovement = false;
        isAttacking = false;
    }*/
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

            monster.PlayAttackAnimation(dir);

            GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity);
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