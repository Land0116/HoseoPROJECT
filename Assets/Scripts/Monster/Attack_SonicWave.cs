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

    private float timer;
    private float trackingTimer;

    private bool isTracking = false;
    private bool isLocked = false;

    private Vector2 lockedDir;

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

        // 1. 공격 시작
        if (!isTracking && dist <= attackRange && timer >= cooldown)
        {
            timer = 0f;
            isTracking = true;
            trackingTimer = trackingTime;

            blockMovement = true; 
        }

        // 2. Tracking
        if (isTracking && !isLocked)
        {
            Vector2 dir = (player.position - monster.transform.position).normalized;

            RotateTowards(dir);

            trackingTimer -= Time.deltaTime;

            if (trackingTimer <= 0f)
            {
                lockedDir = dir;
                isLocked = true;

                StartCoroutine(FireDelayRoutine());
            }
        }
    }

    // 회전 함수
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
        monster.PlayAttackAnimation(lockedDir);

        Vector3 forward = monster.transform.right;
        Vector3 spawnPos = monster.transform.position + forward * 3f;

        GameObject obj = Instantiate(
            sonicPrefab,
            spawnPos,
            monster.transform.rotation * Quaternion.Euler(0, 0, 90f)
        );


        SonicWave wave = obj.GetComponent<SonicWave>();
        if (wave != null)
        {
            wave.onDestroy += OnWaveEnd;
        }


        isTracking = false;
        isLocked = false;
    }


    private void OnWaveEnd()
    {
        blockMovement = false; 
    }
}