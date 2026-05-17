using UnityEngine;
using System.Collections;

public class Attack_SonicWave : AttackPattern
{
    [Header("Set Attack")]
    [SerializeField] private GameObject sonicPrefab;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float cooldown = 2f;

    [Header("SetRotation")]
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float trackingTime = 0.7f;

    [Header("Attack delay")]
    [SerializeField] private float fireDelay = 0.5f;

    [Header("Telegraph (Triangle)")]
    [SerializeField] private GameObject triangleTelegraphPrefab;
    [SerializeField] private float telegraphWidth = 3f;
    private TriangleTelegraph telegraphInstance;

    private float timer;
    private float trackingTimer;

    private bool wasInRange = false;
    private bool isTracking = false;
    private bool isLocked = false;

    private Vector2 lockedDir;

    private Vector3 telegraphSpawnPos;

    private bool isAttacking = false;

    private Vector3 attackOrigin;
    private Vector2 attackDir;


    private void Awake()
    {
        blockMovement = false;
    }

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsHit()) return;

        timer += Time.deltaTime;

        Transform player = monster.Player;
        if (player == null) return;

        float dist = Vector2.Distance(monster.transform.position, player.position);
        bool isInRange = dist <= attackRange;

        if (isInRange && !wasInRange)
            monster.ShowAttackAlert(0.3f);

        wasInRange = isInRange;

        if (!isInRange && !isTracking)
            return;

        if (telegraphInstance != null)
        {
            blockMovement = true;
        }

        if (!isTracking && isInRange && timer >= cooldown)
        {
            timer = 0f;
            isTracking = true;
            trackingTimer = trackingTime;

            blockMovement = true;

            attackOrigin = monster.transform.position;
            attackDir = (player.position - attackOrigin).normalized;

            Vector3 startPos = attackOrigin + (Vector3)(attackDir * 3f);

            float adjustedRange = attackRange - 3f;

            GameObject obj = Object.Instantiate(
                triangleTelegraphPrefab,
                startPos,
                Quaternion.identity
            );

            telegraphInstance = obj.GetComponent<TriangleTelegraph>();
            telegraphInstance.Init(attackDir, adjustedRange, telegraphWidth, fireDelay);
        }

        if (isTracking && !isLocked)
        {
            RotateTowards(attackDir);

            trackingTimer -= Time.deltaTime;

            if (trackingTimer <= 0f)
            {
                lockedDir = attackDir;
                isLocked = true;

                if (telegraphInstance != null)
                    telegraphInstance.StartFill(fireDelay);

                StartCoroutine(FireDelayRoutine());
            }
        }
    }

    private void RotateTowards(Vector2 dir)
    {
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float currentAngle = monster.transform.eulerAngles.z;

        float angle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            rotateSpeed * Time.deltaTime
        );

        monster.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator FireDelayRoutine()
    {
        yield return new WaitForSeconds(fireDelay);
        Fire();
    }

    private void Fire()
    {
        if (telegraphInstance != null)
            Object.Destroy(telegraphInstance.gameObject);

        monster.PlayAttackAnimation(lockedDir);

        Vector3 spawnPos = attackOrigin + (Vector3)(lockedDir * 3f);

        GameObject obj = Object.Instantiate(
            sonicPrefab,
            spawnPos,
            monster.transform.rotation * Quaternion.Euler(0, 0, 90f)
        );

        SonicWave wave = obj.GetComponent<SonicWave>();
        if (wave != null)
        {
            wave.onDestroy += OnWaveEnd;

            blockMovement = true;
        }

        isTracking = false;
        isLocked = false;
    }

    private void OnWaveEnd()
    {
        blockMovement = false;
        isAttacking = false;
    }

}