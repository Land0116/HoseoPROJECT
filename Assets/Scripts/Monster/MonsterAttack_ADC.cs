using System.Collections;
using UnityEngine;

public class MonsterAttack_ADC : AttackPattern
{
    public GameObject bulletPrefab;
    public float fireRate = 1f;
    public float attackRange = 5f;
    public float preDelay = 0.3f;

    private float timer;
    private bool wasInRange = false;
    private bool isAttacking;

    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;

        float distance = Vector2.Distance(monster.Player.position, monster.transform.position);
        bool isInRange = distance <= attackRange;

        if (isInRange && !wasInRange)
        {
            monster.ShowAttackAlert(0.3f);
        }

        if (!isInRange)
        {
            timer = 0f;
            wasInRange = false;
            return;
        }

        timer += Time.deltaTime;

        if (!isAttacking && timer >= 1f / fireRate)
        {
            timer = 0f;
            StartCoroutine(AttackRoutine());
        }

        wasInRange = isInRange;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Vector3 dir = (monster.Player.position - monster.transform.position).normalized;

        monster.SetAnimationLock(false); // 핵심 (Lock 걸면 갱신 막힘 구조 있음)

        monster.PlayAttackPreAnimation(dir);

        yield return new WaitForSeconds(preDelay);

        monster.PlayAttackFireAnimation(dir);

        GameObject bullet = Object.Instantiate(
            bulletPrefab,
            monster.transform.position,
            Quaternion.identity
        );

        bullet.GetComponent<MonsterBullet>().SetDirection(dir);

        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
    }

    private void OnEnable()
    {
        if (monster != null)
            monster.forceADCDeathDuration = true;
    }

    private void OnDisable()
    {
        if (monster != null)
            monster.forceADCDeathDuration = false;
    }
}