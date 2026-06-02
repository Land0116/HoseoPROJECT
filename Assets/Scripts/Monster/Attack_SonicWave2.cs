using System.Collections;
using UnityEngine;

public class Attack_SonicWave2 : AttackPattern
{
    [Header("Attack")]
    [SerializeField] private GameObject sonicWavePrefab;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float spawnOffset = 3f;
    [SerializeField] private float AttackAnimationTime = 1.5f;

    private float timer;
    private bool isAttacking;

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.Player == null) return;
        if (monster.IsHit()) return;
        if (isAttacking) return;

        timer += Time.deltaTime;

        float dist = Vector2.Distance(monster.transform.position, monster.Player.position);

        if (dist > attackRange) return;
        if (timer < cooldown) return;

        timer = 0f;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        monster.blockMovement = true;

        Vector2 playerPos = monster.Player.position;
Vector2 predictedPos = playerPos - monster.Player.GetComponent<Rigidbody2D>().linearVelocity * 0.5f;
Vector2 dir = (predictedPos - (Vector2)monster.transform.position).normalized;

        monster.PlayAttackAnimation(dir);
        monster.SetAnimationLock(true);

        GameObject obj = Instantiate(
            sonicWavePrefab,
            monster.transform.position + (Vector3)(dir * spawnOffset),
            Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg)
        );

        SonicWave2 wave = obj.GetComponent<SonicWave2>();

        if (wave != null)
        {
            yield return wave.ChargeRoutine(AttackAnimationTime);
        }

        monster.SetAnimationLock(false);
        monster.EndAttack();

        monster.blockMovement = false;
        isAttacking = false;
    }
}