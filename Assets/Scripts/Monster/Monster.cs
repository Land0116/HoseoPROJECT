using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    private Transform player;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Vector2 lastMoveDir = Vector2.down;
    private Vector2 lastPosition;

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


    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

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
    
    private void FixedUpdate()
    {
        if (isDead) return;
        if (isKnockbacked) return;
        if (player == null) return;
        Vector2 currentPos = rb.position;
        Vector2 moveDir = (currentPos - lastPosition).normalized;

        // 이동 중일 때만 방향 업데이트
        if (!isAttacking && !isHit ) 
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
        currentHP -= damage;
        //Debug.Log("[" + currentHP + "]" + "남음");
        currentHP = Mathf.Max(currentHP, 0f);

        PlayHitAnimation();

        if (currentHP <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        if (isDead) return;
        isDead = true;

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
        if (animator != null)
        {
            Vector2 dir = lastLookDir;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            string anim = GetDirectionName(angle) + "_Death";

            PlayAnim(anim);

            StartCoroutine(DeathFallbackRoutine());
        }
        else
        {
            DropReward();
            Destroy(gameObject);
        }
    }
    
    private void DropReward()
    {
        if (PlayerController.Instance != null)
        {
            int rewardGold = Random.Range(minGold, maxGold + 1);
            //PlayerController.Instance.Gold += rewardGold;
            float multiplier = PlayerController.Instance.GetGoldMultiplierFromItems();//*
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
    private void UpdateAnimation(Vector2 dir, bool isMoving)
    {
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

        if (dir.magnitude > 0.01f)
            lastLookDir = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        string anim = GetDirectionName(angle) + "_Attack";

        PlayAnim(anim);
    }

    public void PlayHitAnimation()
    {
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
        if (animator == null) return;

        if (animator.runtimeAnimatorController == null) return; 

        if (!HasState(animName)) return;//*

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
            DropReward();
            Destroy(gameObject);
        }
    }

    public void DeathAnimationEvent()
    {
        DropReward();
        Destroy(gameObject);
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
}

