using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class Monster : MonoBehaviour, IDamageable
{
    private Transform player;
    [Header("몬스터 이동중 다른애니메이션 표시 x")]
    private bool isInAttackAnimation = false;

    [Header("Animation Speed")]
    [SerializeField] private bool attackAnimationX2 = false;
    [SerializeField] private float normalAttackAnimSpeed = 1f;
    [SerializeField] private float x2AttackAnimSpeed = 2f;

    [Header("Death Animation Override")]
    [SerializeField] private bool forceFrontDeath = false;
    [SerializeField] private float frontDeathAnimationTime = 2f;

    [Header("Hit Stop Setting")]
    [SerializeField] private bool useHitStop = false;
    [SerializeField] private float hitStopDuration = 1f;

    private bool isHitStopped = false;

    [Header("Hit Knockback Setting")]
    [SerializeField] private bool useHitKnockback = false;
    [SerializeField] private float knockbackDuration = 1f;
    [SerializeField] private float knockbackSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundDelay = 0f;
    [SerializeField] private AudioSource audioSource;
    private Vector2 lastMoveDir = Vector2.down;
    private Vector2 lastPosition;
    
    [Header("보스 보상 스포너")]
    [SerializeField] private RewardObjectSpawner rewardObjectSpawner;
    
    [SerializeField] private Vector3 bossRewardSpawnOffset = new Vector3(0f, 0.5f, 0f);
    [Header("보스 여부")]
    [SerializeField] private bool isBossMonster = false;

    [Header("사망 보상 드랍 여부")]
    [SerializeField] private bool dropRewardOnDeath = true;

    private BossSpawner bossSpawner;
    private bool deathFinished = false;

    [Header("Pattern")]
    [SerializeField] private AttackPattern attackPattern;
    [SerializeField] private MovePattern movePattern;

    [Header("MonsterStat")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxHP = 50f;

    [Header("Death Gold")]
    [SerializeField] private int minGold = 1;
    [SerializeField] private int maxGold = 3;

    [SerializeField] private GameObject hpUIPrefab;
    private MonsterHPUI hpUI;

    private HashSet<SlowFieldInstance> slowFields = new HashSet<SlowFieldInstance>(); //*
    private Dictionary<SlowFieldInstance, float> slowSources = new Dictionary<SlowFieldInstance, float>();

    [Header("Attack View UI")]
    [SerializeField] private GameObject alertUIPrefab;

    private GameObject alertUIInstance;
    private bool isAlertShowing = false;
    //넉백
    private bool isKnockbacked;
    public bool IsKnockbacked => isKnockbacked;
    private float knockbackLockTime = 0.2f;

    private float currentHP;

    private float slowMultiplier = 1f;
    private int slowStack = 0;
    public float CurrentHP => currentHP;
    private Rigidbody2D rb;
    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float MaxHP => maxHP;
    public Rigidbody2D RB => rb;

    [Header("아이템 드랍세팅 0.1은 10퍼")]
    [SerializeField]
    private float weaponPrefabDropChance = 0.3f;
    [SerializeField]
    private float itemDropChance = 0.2f;
    [SerializeField]
    private GameObject weaponPrefab;
    [SerializeField]
    private GameObject itemPrefab;


    [SerializeField]
    private Vector3 weaponSpawner = new Vector3(-0.5f, 0, 0);
    [SerializeField]
    private Vector3 itemSpawner = new Vector3(0.5f, 0, 0);

    //몬스터 애니메이션 부분
    public bool isAttacking = false;
    private string currentAnim;
    private bool isHit = false;
    [SerializeField] private float hitAnimationTime = 0.5f;
    private Vector2 lastLookDir = Vector2.down;
    [SerializeField] private float deathAnimationTime = 0.7f;
    private bool isDead = false;


    private CircleCollider2D col;//콜리더가져오기
    // 플레이어를 기다리는 코루틴 저장용
    private Coroutine bindPlayerRoutine;

    //태엽원숭이 공격 애니메이션 관련
    public bool lockBossFinalAttack = false;
    [Header("Boss Hit Control")]
    public bool ignoreHitAnimation = false;

    private bool isForceDead = false;

    private void OnEnable()
    {
        // 몬스터가 활성화될 때
        // 플레이어가 아직 생성되지 않았을 수도 있으므로
        // 코루틴으로 PlayerController.Instance가 생길 때까지 기다림
        bindPlayerRoutine = StartCoroutine(BindPlayerRoutine());
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되거나 파괴될 때
        // 코루틴이 남아 있으면 정리
        if (bindPlayerRoutine != null)
        {
            StopCoroutine(bindPlayerRoutine);
            bindPlayerRoutine = null;
        }
    }
    
    public void SetBossSpawner(BossSpawner spawner)
    {
        bossSpawner = spawner;
        isBossMonster = true;
    }
    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        currentHP = maxHP;

        if (hpUIPrefab != null)
        {
            GameObject ui = Instantiate(hpUIPrefab, transform);
            ui.transform.localPosition = new Vector3(0, 0.8f, 0);

            hpUI = ui.GetComponent<MonsterHPUI>();

            if (hpUI != null)
            {
                hpUI.Init(this);
            }
        }

        
        if (attackPattern != null)
        {
            attackPattern.Init(this);
        }
        if (movePattern)
        {
            movePattern.Init(this);
        }
        if (rewardObjectSpawner == null)
        {
            rewardObjectSpawner = FindAnyObjectByType<RewardObjectSpawner>();
        }
    }

    void Update()
    {

        if (isDead) return;

        // 플레이어가 씬 전환/삭제/재생성 때문에 잠깐 null 될 수 있으니
        // null이면 다시 연결 시도
        if (player == null && PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
    }
    private void LateUpdate()
    {
        if (alertUIInstance != null)
        {
            alertUIInstance.transform.position = transform.position + new Vector3(1f, 0.2f, 0);
            alertUIInstance.transform.rotation = Quaternion.identity;
        }
    }
    private void FixedUpdate()
    {
        if (isDead || isForceDead) return;
        if (isKnockbacked) return;
        if (isHitStopped) return; //*
        if (player == null) return;
        Vector2 currentPos = rb.position;
        Vector2 moveDir = (currentPos - lastPosition).normalized;

        // 이동 중일 때만 방향 업데이트
        /*if (!isAttacking && !isHit && !lockBossFinalAttack && !ignoreHitAnimation)//* 0529
        {
            if (moveDir.magnitude > 0.01f)
            {
                lastMoveDir = moveDir;
                UpdateAnimation(moveDir, true);
            }
            else
            {
                UpdateAnimation(lastMoveDir, false);
            }
        }*/
        if (!isHit && !lockBossFinalAttack && !ignoreHitAnimation)
        {
            if (moveDir.magnitude > 0.01f)
            {
                lastMoveDir = moveDir;
            }

            // 공격 중이어도 방향은 갱신 가능하게 유지
            UpdateAnimation(lastMoveDir, moveDir.magnitude > 0.01f);
        }

        lastPosition = currentPos;

        if (attackPattern != null)
        {
            attackPattern.Execute();

            if (!attackPattern.blockMovement && movePattern != null)
            {
                movePattern.Execute();
            }
        }
    }
    
    /// <summary>
    /// 플레이어가 준비될 때까지 기다렸다가 연결하는 코루틴
    /// </summary>
    private IEnumerator BindPlayerRoutine()
    {
        // PlayerController.Instance가 생길 때까지 대기
        while (PlayerController.Instance == null)
        {
            yield return null;
        }

        // 플레이어가 준비되면 Transform 연결
        player = PlayerController.Instance.transform;
    }

    //데미지 입음
    public void OnDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        if (currentHP <= 0)
        {
            Death();
            return;
        }

        // 죽는 중이면 Hit 무시
        if (ignoreHitAnimation)
            return;

        PlayHitAnimation();
        TriggerHitKnockback();
    }

    public void Death()
    {
        if (isDead) return;
        isDead = true;
        isForceDead = true;

        currentAnim = ""; //* 추가
        animator.Rebind();
        animator.Update(0f);

        StopAllCoroutines();

        PlayDeathSound();
        isAttacking = false;
        isHit = false;

        rb.linearVelocity = Vector2.zero;
        // 추가: 죽자마자 물리 비활성화
        if (rb != null)
        {
            rb.simulated = false; // 수정
        }
        if (col != null)
        {
            col.enabled = false;
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnKillEnemy();
        }
        // 애니메이션 있으면 재생
        /*if (animator != null)
        {
            Vector2 dir = lastLookDir;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            string anim = GetDirectionName(angle) + "_Death";

            animator.Play(anim, 0, 0f);

            StartCoroutine(DeathFallbackRoutine());
        }*/
        if (animator != null)
        {
            if (forceFrontDeath)
            {

                animator.Play("Front_Death", 0, 0.1f);
                StartCoroutine(DeathRoutine(frontDeathAnimationTime));

            }
            else
            {
                Vector2 dir = lastLookDir;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                string anim = GetDirectionName(angle) + "_Death";

                animator.Play(anim, 0, 0f);
                StartCoroutine(DeathRoutine(deathAnimationTime));

            }

            //StartCoroutine(DeathFallbackRoutine());
        }
        else
        {
            FinishDeath();
        }
    }
    private IEnumerator DeathRoutine(float time)
    {
        yield return new WaitForSeconds(time);

        if (this != null)
        {
            FinishDeath();
        }
    }
    private void FinishDeath()
    {
        if (deathFinished) return;

        deathFinished = true;

        if (isBossMonster)
        {
            DropReward();
            HandleBossDeathReward();
        }

        else
        {
            if (dropRewardOnDeath)
            {
                DropReward();
            }
        }

        Destroy(gameObject);
    }

    private void DropReward()
    {
        if (PlayerController.Instance != null)
        {
            int rewardGold = Random.Range(minGold, maxGold + 1);
            //PlayerController.Instance.Gold += rewardGold;
            float multiplier = PlayerController.Instance.GetGoldMultiplierFromItems(); //*
            int finalGold = Mathf.RoundToInt(rewardGold * multiplier);

            PlayerController.Instance.Gold += finalGold;
        }

        if (Random.value < weaponPrefabDropChance)
        {
            Instantiate(weaponPrefab, transform.position + weaponSpawner, Quaternion.identity);
        }

        if (Random.value < itemDropChance)
        {
            Instantiate(itemPrefab, transform.position + itemSpawner, Quaternion.identity);
        }
       
    }
    private void HandleBossDeathReward()
    {
        if (bossSpawner != null && !bossSpawner.ShouldDropBossReward())
        {
            Debug.Log("[Monster] 최종 보스이므로 보스 보상 드롭 생략");
            NotifyBossDead();
            return;
        }

        if (rewardObjectSpawner == null)
        {
            rewardObjectSpawner = FindAnyObjectByType<RewardObjectSpawner>();
        }

        if (rewardObjectSpawner == null)
        {
            Debug.LogWarning("[Monster] RewardObjectSpawner가 없음. 보스 클리어만 처리함.");
            NotifyBossDead();
            return;
        }

        rewardObjectSpawner.TrySpawnBossHealthItem(transform.position);

        RewardType rewardType = rewardObjectSpawner.GetRandomBossRewardType();

        Vector3 spawnPosition = transform.position + bossRewardSpawnOffset;

        rewardObjectSpawner.SpawnRewardObjectAtPosition(
            rewardType,
            spawnPosition,
            () =>
            {
                NotifyBossDead();
            }
        );

        Debug.Log($"[Monster] 보스 보상 생성: {rewardType}");
    }

    private void NotifyBossDead()
    {
        if (bossSpawner != null)
        {
            bossSpawner.NotifyBossDead();
            return;
        }

        FindAnyObjectByType<BossSpawner>()?.NotifyBossDead();
    }
    
    
    private void UpdateAnimation(Vector2 dir, bool isMoving)
    {
        if (isInAttackAnimation) return; //* 0601

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle);

        if (isMoving)
            PlayAnim(anim + "_Walk");
        else
            PlayAnim(anim + "_Idle");
    }

    private string GetDirectionName(float angle)
    {
        if (angle >= -22.5f && angle < 22.5f)//오른쪽
            return "SideR";
        if (angle >= 22.5f && angle < 67.5f)// 오른쪽 위
            return "QBackR";
        if (angle >= 67.5f && angle < 112.5f)// 위
            return "Back";
        if (angle >= 112.5f && angle < 157.5f)//왼쪽 위
            return "QBackL";
        if (angle >= 157.5f || angle < -157.5f)// 왼
            return "SideL";
        if (angle >= -157.5f && angle < -112.5f)//왼 아래
            return "QFrontL";
        if (angle >= -112.5f && angle < -67.5f)//아래
            return "Front";
        if (angle >= -67.5f && angle < -22.5f)//오른 아래
            return "QFrontR";

        return "Front";
    }

    public void PlayAttackAnimation(Vector2 dir)
    {
        if (isHit) return;

        isInAttackAnimation = true; //* 0601

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        SetAttackAnimSpeed();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Attack";

        PlayAnim(anim);
    }

    public void PlayHitAnimation()
    {
        if (ignoreHitAnimation) return;
        if (isInAttackAnimation) return; //* 0601

        Vector2 dir = lastLookDir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Hit";

        isHit = true;
        isAttacking = false;

        rb.linearVelocity = Vector2.zero;

        PlayAnim(anim);

        StartCoroutine(HitRoutine());
    }
    private IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(hitAnimationTime);
        
        isHit = false;
    }
    public bool IsHit()
    {
        return isHit;
    }
    void PlayAnim(string animName)
    {
        if (isForceDead) return;

        if (animator == null) return;

        if (animator.runtimeAnimatorController == null) return; 

        if (!HasState(animName)) return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Front_Death"))
            return;

        if (lockBossFinalAttack) //* 0529 보스
        {
            if (!animName.Contains("_Attack"))
                return;
        }

        if (currentAnim == animName) return;

        currentAnim = animName;
        animator.Play(animName);
    }
    bool HasState(string stateName) //*
    {
        int hash = Animator.StringToHash(stateName);

        return animator.HasState(0, hash);
    }
    IEnumerator DeathFallbackRoutine()
    {
        yield return new WaitForSeconds(deathAnimationTime);

        if (this != null)
        {
            FinishDeath();
        }
    }

    public void DeathAnimationEvent()
    {
        FinishDeath();
    }

    public void DeathAnimationStartEvent()
    {
        //콜라이더 제거
        if (col != null)
            col.enabled = false;
        //rb같이 제거
        if (rb != null)
            rb.simulated = false;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed * slowMultiplier;
    }
    public float GetSpeedRatio()
    {
        return slowMultiplier;
    }

    public void AddSlow(SlowFieldInstance source, float multiplier)
    {
        if (slowSources.ContainsKey(source)) return;

        slowSources[source] = multiplier;
        RecalculateSlow();
    }

    public void RemoveSlow(SlowFieldInstance source)
    {
        if (!slowSources.ContainsKey(source)) return;

        slowSources.Remove(source);
        RecalculateSlow();
    }
    private void RecalculateSlow()
    {
        slowMultiplier = 1f;

        foreach (var s in slowSources.Values)
        {
            slowMultiplier *= s;
        }
    }
    public bool IsSlowedBy(SlowFieldInstance source)
    {
        return slowSources.ContainsKey(source);
    }
    public void ApplyKnockback(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(KnockbackLock());
    }

    IEnumerator KnockbackLock()
    {
        isKnockbacked = true;
        yield return new WaitForSeconds(0.2f);
        isKnockbacked = false;
    }
    public bool IsMovementLocked()
    {
        return isKnockbacked || isHit;
    }

    public Vector2 GetLookDirection()
    {
        return lastLookDir;
    }
    public void ShowAttackAlert(float duration = 0.8f)
    {
        if (alertUIPrefab == null) return;
        if (isAlertShowing) return;

        isAlertShowing = true;

        alertUIInstance = Instantiate(alertUIPrefab, transform);
        alertUIInstance.transform.localPosition = new Vector3(1f, 0.2f, 0);

        StartCoroutine(AlertRoutine(duration));
    }

    private IEnumerator AlertRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (alertUIInstance != null)
        {
            Destroy(alertUIInstance);
        }

        isAlertShowing = false;
    }
    private void PlayDeathSound()
    {
        if (deathSound == null || audioSource == null) return;

        if (deathSoundDelay <= 0f)
        {
            PlayIndependent(deathSound);
        }
        else
        {
            StartCoroutine(PlayDeathSoundDelayRoutine());
        }
    }
    private IEnumerator PlayDeathSoundDelayRoutine()
    {
        yield return new WaitForSeconds(deathSoundDelay);

        PlayIndependent(deathSound);
    }
    private void PlayIndependent(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        GameObject obj = new GameObject("Monster_Death_SFX");
        AudioSource newSource = obj.AddComponent<AudioSource>();

        newSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        newSource.volume = audioSource.volume;
        newSource.pitch = audioSource.pitch;
        newSource.spatialBlend = 0f;

        newSource.clip = clip;
        newSource.Play();

        Destroy(obj, clip.length + 0.1f);
    }
    public void TriggerHitKnockback()
    {
        if (!useHitKnockback) return;

        StopCoroutine(nameof(HitKnockbackRoutine));
        StartCoroutine(HitKnockbackRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        isHitStopped = true;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(hitStopDuration);

        isHitStopped = false;
    }
    private IEnumerator HitKnockbackRoutine()
    {
        isHitStopped = true;

        float timer = 0f;

        while (timer < knockbackDuration)
        {
            if (player != null)
            {
                Vector2 dir = (rb.position - (Vector2)player.position).normalized;

                rb.linearVelocity = dir * knockbackSpeed;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isHitStopped = false;
    }
    public void ForceLookAt(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

        if (dir.magnitude < 0.01f) return;

        lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Idle";

        PlayAnim(anim);
    }
    public void PlayBossDashAnimation(Vector2 dir)
    {
        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Dash";

        PlayAnim(anim);
    }

    public void PlayBossFinalAttackAnimation(Vector2 dir)
    {
        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Attack";

        PlayAnim(anim);
    }

    public void PlayMissilePrepareAnimation(Vector2 dir)
    {
        if (isHit) return;

        isInAttackAnimation = true;//* 0601

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Missile_Pre";

        PlayAnim(anim);
    }

    public void PlayMissileAnimation(Vector2 dir)
    {
        if (isHit) return;

        isInAttackAnimation = true; //* 0601

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Missile";

        PlayAnim(anim);
    }
    public void SetAttackAnimationLock(bool value)
    {
        isInAttackAnimation = value;
        if (!value)
        {
            ResetAnimSpeed();
        }
    }
    private void SetAttackAnimSpeed()
    {
        if (animator == null) return;

        animator.speed = attackAnimationX2 ? x2AttackAnimSpeed : normalAttackAnimSpeed;
    }
    private void ResetAnimSpeed()
    {
        if (animator == null) return;
        animator.speed = 1f;
    }

    public void EndAttack()
    {
        isInAttackAnimation = false;
        ResetAnimSpeed();
        SetAttackAnimationLock(false);
    }

}

