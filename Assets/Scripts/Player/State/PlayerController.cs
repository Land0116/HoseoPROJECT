using System;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour, IDamageable
{
    public static PlayerController Instance;

    public enum AttackType
    {
        Base, //단추
        Item //아이템
    }

    public enum PlayerState
    {
        Idle, //대기
        Walk, //걷기
        Attack, //타격
        Hit, //피격
        Death //죽음
    }


    [Header("플레이어 기본 정보")] [SerializeField]
    private float baseMaxHp = 35; // 게임 시작 시 기준 최대 체력

    [SerializeField] private float baseMoveSpeed = 8.0f; // 게임 시작 시 기준 이동속도
    [SerializeField] private float baseAttackPerSecond = 1.0f; // 게임 시작 시 기준 초당 공격 횟수

    [Header("플레이어 현재 상태값")] [SerializeField]
    private float hp; // 현재 체력

    [SerializeField] public float maxHp; // 현재 최대 체력
    [SerializeField] private int gold = 10; // 현재 소지 골드
    [SerializeField] private float moveSpeed; // 현재 이동속도
    [SerializeField] public float attackPerSecond = 1.0f; //발사 주기 바뀜*
    [Header("총알 생성 보정")] [SerializeField] private float bulletSpawnOffset = 0.2f;

    [Header("플레이어 상태 제어")] [SerializeField]
    private PlayerState playerState = PlayerState.Idle; // 현재 상태머신 상태
    
    [SerializeField] private bool isDie = false; // 사망 여부
    [SerializeField] private bool isFireInput = false; //발사입력
    [SerializeField] private bool canControl = true; // 조작 가능 여부

    [Header("비주얼 / 애니메이션")] [SerializeField]
    private Animator bodyAnimator;
    [Header("차지샷 애니메이션")]
    [SerializeField] private bool isChargingShot = false;

// 현재 마우스를 향하는 방향 벡터
    [SerializeField] private Vector2 aimDirection = Vector2.down;
    [SerializeField] private Vector2 animDirection = Vector2.down;
    [Header("공격 잠금 방향")] [SerializeField] private Vector2 lockedAttackAimDirection = Vector2.down;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMoveHash = Animator.StringToHash("IsMove");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int IsDeathHash = Animator.StringToHash("IsDeath");
    private static readonly int IsChargeHash = Animator.StringToHash("IsCharge");

    [Header("피격")] [SerializeField] private bool isHitAnimating = false;

    [Header("대쉬")] [SerializeField] private Vector2 lastMoveDirection = Vector2.down; // 마지막 이동 방향 저장
    [SerializeField] private float dashDistance; // 짧게 이동할 거리
    [SerializeField] private float dashDuration = 0.3f; // 대쉬 지속 시간
    [SerializeField] private float dashCooldown = 2.0f; // 쿨타임 2초(임시)

    [SerializeField] private bool isDashing = false; // 현재 대쉬 중인지
    [SerializeField] private Vector2 dashDirection = Vector2.down; // 대쉬 방향
    [SerializeField] private float dashEndTime = -999f; // 대쉬 종료 시각
    [SerializeField] private float lastDashTime = -999f; // 마지막 대쉬 사용 시각

    [Header("장비")] [SerializeField] private WeaponData basicWeapon; // 시작 무기
    [SerializeField] private WeaponData currentWeapon; //현재 장착 중인 총알

    [SerializeField] private ItemData currentItem; // 현재 장착 중인 아이템
    [SerializeField] private GameObject curProjectilePrefab; // 현재 발사할 투사체 프리팹
    [SerializeField] private float weaponDamage; // 무기 기본 공격력 (기존 attackDamage를 무기 공격력으로 사용)
    [SerializeField] public float itemDamage = 0f; // 아이템 공격력 //*

    [Header("스킬 배수")] //스킬 관련
    private float attackMultiplier = 1f; //공격력 증가
    private bool shieldActive = false; // 보호막
    private bool speedBuffActive = false; //속도 버프
    //아이템 5개까지 장착
    private List<ItemData> equippedItems = new List<ItemData>();
    private const int MAX_ITEM_COUNT = 5;
    [SerializeField] private GameObject itemPickupPrefab;

    //조건부 아이템
    [HideInInspector] public bool lowHp30Active;
    [HideInInspector] public bool lowHp20Active;
    [HideInInspector] public bool highGoldActive;
    [HideInInspector] public bool highAttackSpeedActive;
    private bool statsDirty = false;
    public bool attackSpeed6Active; 
    public bool gold100Active; //100골드 이상 보유시
    public bool hasDisplayTicket;

    //몬스터 스킬 넉백
    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackDrag = 8f;

    private bool isKnockback;
    private float knockbackEndTime;

    #region 증강 - 서브 스킬형

    [Header("증강 - 서브 스킬 / 현재 장착 증강")] [SerializeField]
    private AugmentationSystem currentShotAugment;

    [SerializeField] private AugmentationSystem currentTrajectoryAugment;
    [SerializeField] private AugmentationSystem currentEffectAugment;

    [SerializeField] private int currentShotLevel = 0;
    [SerializeField] private int currentTrajectoryLevel = 0;
    [SerializeField] private int currentEffectLevel = 0;

    [Header("증강 - 서브 스킬 / 발사")] [SerializeField]
    private AugmentationSystem.ShotFireMode shotFireMode = AugmentationSystem.ShotFireMode.Single;

    [SerializeField] private int shotProjectileCountAdd = 0; // 기본 1발에서 추가
    [SerializeField] private float shotDamageMultiplier = 1f; // 발사 구조 데미지 배율
    [SerializeField] private float shotSpreadAngle = 0f; // 부채꼴 발사 각도
    [SerializeField] private float shotChargeTime = 0f; // 차지샷용
    [SerializeField] private float shotBurstInterval = 0f; // 버스트 간격

    [Header("증강 - 3연발")] [SerializeField] private bool burstEnabled = false;
    [SerializeField] private int burstProjectileCount = 1;
    [SerializeField] private float burstInterval = 0.12f;

    [Header("증강 - 산탄")] [SerializeField] private bool multiShotEnabled = false;
    [SerializeField] private int multiShotProjectileCount = 1;
    [SerializeField] private float multiShotSpreadAngle = 0f;

    [Header("증강 - 차지샷")] [SerializeField] private bool chargeShotEnabled = false;
    [SerializeField] private float chargeShotTime = 0f;
    [SerializeField] private float chargeShotDamageMultiplier = 1f;

    [Header("증강 - 공격 시퀀스")] [SerializeField]
    private bool isAttackSequenceRunning = false;

    [Header("증강 - 주위탄 편대 배치")] [SerializeField]
    private float orbitFormationSpacing = 0.35f;

    [SerializeField] private float orbitStartAngleOffset = 90f;
    [SerializeField] private int maxOrbitProjectileTotal = 40;
    

    [Header("증강 - 서브 스킬 / 궤적")] [SerializeField]
    private float trajectoryHomingStrength = 0f;

    [SerializeField] private float trajectoryDuration = 0f;
    [SerializeField] private int trajectoryBounceCount = 0;
    [SerializeField] private int trajectoryPierceCount = 0;

    [Header("증강 - 서브 스킬 / 효과")] [SerializeField]
    private float effectExplosionRadius = 0f;

    [SerializeField] private float effectExplosionDamageMultiplier = 1f;
    [SerializeField] private int effectChainCount = 0;
    [SerializeField] private float effectChainRange = 0f;
    [SerializeField] private float effectDotDamagePerSecond = 0f;
    [SerializeField] private float effectDotDuration = 0f;

    [Header("증강 - 서브 스킬 / 주위탄")] [SerializeField]
    private GameObject orbitProjectilePrefab; // 주위탄 전용 프리팹

    [SerializeField] private bool shotUseOrbitProjectile = false;
    [SerializeField] private int shotOrbitProjectileCount = 0;
    [SerializeField] private float shotOrbitRadius = 1.5f;
    [SerializeField] private float shotOrbitAngularSpeed = 180f;
    [SerializeField] private float shotOrbitHitInterval = 0.2f;
    [SerializeField] private float shotOrbitDamageMultiplier = 1f;
    [SerializeField] private float shotOrbitLifetime = 0f;

    
    // 현재 유지 중인 주위탄들
    private readonly List<OrbitProjectile> activeOrbitProjectiles = new List<OrbitProjectile>();

    #endregion

    #region 증강 - 서브 스킬 추가 런타임값

    [Header("증강 - 서브 스킬 / 추가 적용값")] [SerializeField]
    private float shotAttackSpeedBonusPercent = 0f;

    [SerializeField] private float shotProjectileLifeMultiplier = 1f;

    #endregion

    #region 증강 - 패시브형

    [Header("증강 - 패시브 / 최종 적용값")] [SerializeField]
    private float passiveDamageMultiplier = 1f;

    [SerializeField] private float passiveAttackSpeedPercent = 0f;
    [SerializeField] private int passiveMaxHpAdd = 0;
    [SerializeField] private float passiveLifeStealPercent = 0f;
    
    #region 기본 공격 입력 상태
    // 기본 공격 1회 발사용 입력 버퍼
    [SerializeField] private bool requestSingleShot = false;

    // 현재 누르고 있는 입력에서 이미 발사했는지
    [SerializeField] private bool firedThisPress = false;

    #endregion
    
    #endregion

    #region 증강 - 특수형

    [Header("증강 - 특수형 / 최종 적용값")] [SerializeField]
    private float specialMoveSpeedMultiplier = 1f;

    [SerializeField] private float specialMoveSpeedAdd = 0f;
    [SerializeField] private float specialCharacterScaleMultiplier = 1f;
    [SerializeField] private bool specialIgnoreObstacleCollision = false;

    [SerializeField] private bool specialCheatDeath = false;
    [SerializeField] private bool specialCheatDeathUsed = false;
    [SerializeField] private float specialCheatDeathHp = 1;
    [SerializeField] private float specialCheatDeathInvincibleDuration = 1f;

    [SerializeField] private float specialHpToDamagePercentPer10Hp = 0f;
    [SerializeField] private float specialHealOnKill = 0f;
    [SerializeField] private float specialHpRegenPerSecond = 0f;

    [SerializeField] private float specialHealToAutoAttackThreshold = 0f;
    [SerializeField] private float specialHealToAutoAttackDamageMultiplier = 0f;

    [SerializeField] private float specialShieldInterval = 0f;
    [SerializeField] private int specialShieldMaxCount = 0;
    [SerializeField] private int currentShieldCount = 0;
    [Header("스페셜 - 보호막 시각 효과")]
    [SerializeField] private GameObject shieldVisualObject;

    [SerializeField] private string obstacleLayerName = "Obstacle";

    private float nextShieldChargeTime = -1f;
    private float invincibleUntilTime = -1f;
    private float hpRegenAccumulator = 0f;
    private float healAccumulatorForAutoAttack = 0f;

    private Vector3 defaultPlayerScale = Vector3.one;
    private Collider2D playerCollider2D;

    #endregion

    private float chargeStartTime = -1f;

    [Header("컴포넌트 참조")] [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 inputDirection;
    private Camera _mainCamera;

    [Header("조준점 설정")] [SerializeField] private Transform crosshairTransform; // 계층 구조의 Crosshair 오브젝트 연결
    [SerializeField] private bool hideSystemCursor = true; // 시스템 커서 숨김 여부
    [SerializeField] private Transform playerBody;

    [Header("총알 소환 위치 관련")] public Transform gunTip;

    [SerializeField] private float deathUIShowDelay = 0.5f; // 죽음 애니메이션 종료 후 UI 표시 지연 시간
    private Coroutine burstShootCoroutine;
    private Coroutine deathUICoroutine;
    private bool isPointerOverUIThisFrame = false;

    //수정하면서 추가한 부분
    private float nextAttackTime = 0f;

    public float Hp
    {
        get => hp;
        set => hp = Mathf.Clamp(value, 0, maxHp);
    }

    public float MaxHp => maxHp;
    /// <summary>
    /// UI 표시용 현재 체력.
    /// 실제 체력은 float지만, UI에는 int처럼 보여준다.
    /// 체력이 0.1 남아있는데 0으로 표시되면 죽은 것처럼 보이므로 CeilToInt를 사용한다.
    /// </summary>
    public int DisplayHp
    {
        get
        {
            if (Hp <= 0f) return 0;
            return Mathf.CeilToInt(Hp);
        }
    }

    /// <summary>
    /// UI 표시용 최대 체력.
    /// </summary>
    public int DisplayMaxHp
    {
        get
        {
            return Mathf.CeilToInt(MaxHp);
        }
    }

    /// <summary>
    /// HP바 전용 비율.
    /// </summary>
    public float HpRatio
    {
        get
        {
            if (MaxHp <= 0f) return 0f;
            return Mathf.Clamp01(Hp / MaxHp);
        }
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0, value);
    }

    public int Gold
    {
        get => gold;
        set => gold = Mathf.Max(0, value);
    }


    public float AttackPerSecond
    {
        get => attackPerSecond;
        set => attackPerSecond = Mathf.Max(0.1f, value);
    }


    private float DashSpeed
    {
        get
        {
            if (dashDuration <= 0f) return 0f;
            return dashDistance / dashDuration;
        }
    }
    public void SetAttackMultiplier(float value)
    {
        attackMultiplier = Mathf.Max(0f, value);
    }
    public void SetShieldState(bool value)
    {
        shieldActive = value;
    }
    public void SetSpeedBuffState(bool value)
    {
        speedBuffActive = value;
    }
    public void ApplySpeedBuffMultiplier(float multiplier)
    {
        moveSpeed = baseMoveSpeed * multiplier;
        attackPerSecond = baseAttackPerSecond * multiplier;
    }

    public bool IsDashOnCooldown()
    {
        return Time.time < lastDashTime + dashCooldown;
    }

    public float GetDashCooldownRemain()
    {
        return Mathf.Max(0f, (lastDashTime + dashCooldown) - Time.time);
    }

    public float GetDashCooldownRatio()
    {
        if (dashCooldown <= 0f) return 0f;
        return Mathf.Clamp01(GetDashCooldownRemain() / dashCooldown);
    }

    public bool IsDie
    {
        get => isDie;
        set => isDie = value;
    }

    public bool IsHitAnimating
    {
        get => isHitAnimating;
        set => isHitAnimating = value;
    }

    #region 증강 Getter

    public int FinalProjectileCount => Mathf.Max(1, 1 + shotProjectileCountAdd);
    public float FinalShotSpreadAngle => shotSpreadAngle;
    public float FinalShotChargeTime => shotChargeTime;

    public int FinalBurstCount
    {
        get { return burstEnabled ? Mathf.Max(1, burstProjectileCount) : 1; }
    }

    public int FinalMultiShotCount
    {
        get { return multiShotEnabled ? Mathf.Max(1, multiShotProjectileCount) : 1; }
    }

    public float FinalMultiShotSpreadAngle
    {
        get { return multiShotEnabled ? multiShotSpreadAngle : 0f; }
    }

    public float FinalChargeShotTime
    {
        get { return chargeShotEnabled ? chargeShotTime : 0f; }
    }

    public int FinalOrbitColumnCount
    {
        get
        {
            // 주위탄 개수 배율.
            // 3연발과 산탄이 둘 다 있으면 둘을 곱한다.
            return Mathf.Max(1, FinalBurstCount * FinalMultiShotCount);
        }
    }

    public float FinalHomingStrength => trajectoryHomingStrength;
    public float FinalTrajectoryDuration => trajectoryDuration;
    public int FinalBounceCount => trajectoryBounceCount;
    public int FinalPierceCount => trajectoryPierceCount;

    public float FinalExplosionRadius => effectExplosionRadius;
    public float FinalExplosionDamageMultiplier => effectExplosionDamageMultiplier;
    public int FinalChainCount => effectChainCount;
    public float FinalChainRange => effectChainRange;
    public float FinalDotDamagePerSecond => effectDotDamagePerSecond;
    public float FinalDotDuration => effectDotDuration;

    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider2D = GetComponent<Collider2D>();
        defaultPlayerScale = transform.localScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0f;


        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _mainCamera = Camera.main;

        if (bodyAnimator == null && playerBody != null)
        {
            bodyAnimator = playerBody.GetComponent<Animator>();
        }

        maxHp = baseMaxHp; //체력수정.아이템
        Hp = maxHp;


        if (basicWeapon != null)
        {
            ApplyWeapon(basicWeapon);
        }

        moveSpeed = baseMoveSpeed; //아이템관련 수정
        if (currentItem != null) //수정한 부분.아이템
        {
            ApplyItem(currentItem);
        }
    }


    void Start()
    {
        if (hideSystemCursor) Cursor.visible = false;
        if (bodyAnimator != null)
        {
            bodyAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            bodyAnimator.SetBool(IsMoveHash, false);
            bodyAnimator.SetBool(IsDeathHash, false);
            bodyAnimator.SetFloat(MoveXHash, animDirection.x);
            bodyAnimator.SetFloat(MoveYHash, animDirection.y);
        }

        RebuildPlayerStats();
        if (AugUIManager.instance != null && AugmentRunManager.Instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }

        ApplyWeapon(basicWeapon);
        ApplyItem(currentItem);

        SyncLocomotionState();
    }

    // Update is called once per frame
    void Update()
    {
        HandleKnockback(); //* 0513

        if (IsDie)
            return;

        isPointerOverUIThisFrame = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        foreach (var item in equippedItems)
        {
            item?.OnUpdate(this); //*
        }
        if (statsDirty) //*
        {
            statsDirty = false;
            RebuildPlayerStats();
        }

        UpdateAnimatorLocomotion();
        CheckAttackStateRelease();

        TickSpecialRuntime();

        HandleAttack();
        UpdateAnimatorPlaybackSpeed();
    }

    void LateUpdate()
    {
        PlayerMouseMovement();
    }

    void FixedUpdate()
    {
        HandleKnockback();
        if (isKnockback)
        {
            return;//*

        }

        if (IsDie)
        {
            rb.linearVelocity = Vector2.zero;
            playerState = PlayerState.Death;
            return;
        }

        if (!canControl)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
        {
            if (Time.time >= dashEndTime)
            {
                EndDash();
            }
            else
            {
                rb.linearVelocity = dashDirection * DashSpeed;
                return;
            }
        }

        switch (playerState)
        {
            case PlayerState.Idle:
            {
                rb.linearVelocity = Vector2.zero;

                if (inputDirection.sqrMagnitude > 0.01f)
                {
                    playerState = PlayerState.Walk;
                }

                break;
            }
            case PlayerState.Walk:
            {
                Vector2 moveVector = inputDirection;

                if (moveVector.magnitude > 1f)
                    moveVector.Normalize();

                rb.linearVelocity = moveVector * moveSpeed;

                if (inputDirection.sqrMagnitude <= 0.01f)
                {
                    rb.linearVelocity = Vector2.zero;
                    playerState = PlayerState.Idle;
                }

                break;
            }
            case PlayerState.Attack:
            {
                Vector2 moveVector = inputDirection;

                if (moveVector.magnitude > 1f)
                    moveVector.Normalize();

                rb.linearVelocity = moveVector * moveSpeed;
                break;
            }
            case PlayerState.Hit:
            {
                rb.linearVelocity = Vector2.zero;
                break;
            }
            case PlayerState.Death:
            {
                rb.linearVelocity = Vector2.zero;
                break;
            }
        }
    }


    #region 플레이어 마우스 움직임

    void PlayerMouseMovement()
    {
        if (IsDie) return;
        if (crosshairTransform == null || playerBody == null) return;
        if (_mainCamera == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            -_mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;

        crosshairTransform.position = mouseWorldPos;

        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)playerBody.position);
        if (direction.sqrMagnitude > 0.0001f)
        {
            aimDirection = direction.normalized;
            if (playerState != PlayerState.Attack &&
                playerState != PlayerState.Hit &&
                playerState != PlayerState.Death)
            {
                animDirection = aimDirection;
                ApplyBlendTreeDirection(animDirection);
            }
        }
    }

    #endregion

    #region 애니메이션 관련 함수

    private void ApplyBlendTreeDirection(Vector2 dir)
    {
        if (bodyAnimator == null) return;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.down;

        bodyAnimator.SetFloat(MoveXHash, dir.x);
        bodyAnimator.SetFloat(MoveYHash, dir.y);
    }

    private void SyncLocomotionState()
    {
        bool hasMoveInput =
            !IsDie &&
            canControl &&
            !isDashing &&
            inputDirection.sqrMagnitude > 0.01f;

        // 상태머신 동기화
        if (playerState != PlayerState.Attack &&
            playerState != PlayerState.Hit &&
            playerState != PlayerState.Death)
        {
            playerState = hasMoveInput ? PlayerState.Walk : PlayerState.Idle;
        }

        // Animator의 IsMove는 "실제 이동 입력" 기준으로만 넣는다.
        if (bodyAnimator != null)
        {
            bodyAnimator.SetBool(IsMoveHash, hasMoveInput);
        }
    }

    private void UpdateAnimatorLocomotion()
    {
        if (bodyAnimator == null) return;

        bool hasMoveInput =
            !IsDie &&
            canControl &&
            !isDashing &&
            inputDirection.sqrMagnitude > 0.01f;

        bodyAnimator.SetBool(IsMoveHash, hasMoveInput);
    }

    private bool IsAnimatorInAttackState()
    {
        if (bodyAnimator == null) return false;

        AnimatorStateInfo currentState = bodyAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Attack"))
            return true;

        if (bodyAnimator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = bodyAnimator.GetNextAnimatorStateInfo(0);

            if (nextState.IsTag("Attack"))
                return true;
        }

        return false;
    }

    private void CheckAttackStateRelease()
    {
        if (playerState != PlayerState.Attack)
            return;

        // 아직 Attack Tag 상태면 유지
        if (IsAnimatorInAttackState())
            return;

        // Animator는 이미 공격 상태를 빠져나왔는데
        // 코드만 Attack에 남아있으면 여기서 해제
        ReleaseAttackState();
    }

    private void UpdateAnimatorPlaybackSpeed()
    {
        if (bodyAnimator == null)
            return;

        // 죽음 / 피격 / 일반 이동 / 대기 상태는 항상 기본 속도
        if (playerState == PlayerState.Hit || playerState == PlayerState.Death)
        {
            bodyAnimator.speed = 1f;
            return;
        }

        if (playerState == PlayerState.Walk)
        {
            bodyAnimator.speed = MoveSpeed;
        }

        // Attack 상태이면서 실제 Animator도 Attack Tag 상태일 때만 공격속도 반영
        if (playerState == PlayerState.Attack && IsAnimatorInAttackState())
        {
            bodyAnimator.speed = AttackPerSecond;
            return;
        }

        // 그 외에는 기본 속도
        bodyAnimator.speed = 1f;
    }

    #endregion


    public void EndHitAnimationEvent()
    {
        if (IsDie) return;

        IsHitAnimating = false;

        playerState = inputDirection.sqrMagnitude > 0.01f
            ? PlayerState.Walk
            : PlayerState.Idle;

        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            animDirection = aimDirection;
            ApplyBlendTreeDirection(animDirection);
        }

        SyncLocomotionState();
    }

    /// <summary>
    /// 공격 입력 처리.
    /// 
    /// 주위탄은 상시 회전하는 보조 공격이고,
    /// 마우스 공격은 별개의 기본 공격이다.
    /// 
    /// 따라서 주위탄을 보유했다고 해서
    /// 일반 공격을 막으면 안 된다.
    /// </summary>
    private void HandleAttack()
    {
        // 주위탄은 일반 공격 대체형이다.
        // 따라서 마우스 입력으로 일반 총알이 나가면 안 된다.
        if (shotUseOrbitProjectile)
        {
            CancelNormalAttackBecauseOrbit();
            return;
        }

        if (!CanStartAttackNow()) return;

        if (isFireInput)
        {
            HandleAutoFireAttack();
            return;
        }

        HandleSingleClickAttack();
    }


    /// <summary>
    /// 마우스 클릭 1회 공격 처리.
    /// 
    /// 역할:
    /// - 짧게 클릭한 입력이 Update 타이밍 때문에 씹히지 않게 처리한다.
    /// - 공격속도 쿨타임이 끝났을 때 단발 1회를 실행한다.
    /// - 꾹 누르는 자동연사는 HandleAutoFireAttack()이 담당한다.
    /// </summary>
    private void HandleSingleClickAttack()
    {
        if (!requestSingleShot)
            return;

        if (firedThisPress)
            return;

        // 차지샷은 누르고 있어야 차지가 진행된다.
        // 짧게 클릭 후 뗀 상태라면 차지샷은 취소한다.
        if (FinalChargeShotTime > 0f)
        {
            if (!isFireInput)
            {
                requestSingleShot = false;
                EndChargeShot();
                return;
            }

            if (chargeStartTime < 0f)
                BeginChargeShot();

            if (!IsChargeReady())
                return;
        }

        if (Time.time < nextAttackTime)
            return;

        EndChargeShot();

        FireCurrentAttackPattern();

        requestSingleShot = false;
        firedThisPress = true;
    }

    /// <summary>
    /// 마우스를 누르고 있는 동안 attackPerSecond에 맞춰 자동연사한다.
    /// 
    /// 자동연사는 더 이상 증강 효과가 아니다.
    /// 플레이어 기본 공격 입력 방식이다.
    /// </summary>
    private void HandleAutoFireAttack()
    {
        if (!isFireInput)
        {
            EndChargeShot();
            return;
        }

        if (FinalChargeShotTime > 0f)
        {
            if (chargeStartTime < 0f)
                BeginChargeShot();

            if (!IsChargeReady())
                return;
        }

        if (Time.time < nextAttackTime)
            return;

        EndChargeShot();

        FireCurrentAttackPattern();

        requestSingleShot = false;
        firedThisPress = true;

        // 차지샷을 꾹 누르고 있으면 다음 차지를 바로 다시 시작
        if (FinalChargeShotTime > 0f && isFireInput)
        {
            BeginChargeShot();
        }
    }

    /// <summary>
    /// 현재 발사 조합에 따라 공격 시퀀스를 시작한다.
    /// 
    /// 핵심:
    /// - 단발, 자동연사 모두 이 함수를 사용한다.
    /// - 실제 공격 간격은 GetCurrentAttackInterval()에서 attackPerSecond 기준으로 통일한다.
    /// - 3연발이 있으면 burst 횟수만큼 반복한다.
    /// - 산탄이 있으면 각 burst마다 여러 발 발사한다.
    /// </summary>
    private void FireCurrentAttackPattern()
    {
        EndChargeShot();

        if (shotUseOrbitProjectile)
        {
            CancelNormalAttackBecauseOrbit();
            return;
        }

        if (isAttackSequenceRunning)
            return;

        float attackInterval = GetCurrentAttackInterval();

        float burstTotalTime = burstEnabled
            ? burstInterval * Mathf.Max(0, FinalBurstCount - 1)
            : 0f;

        nextAttackTime = Time.time + burstTotalTime + attackInterval;

        if (burstShootCoroutine != null)
        {
            StopCoroutine(burstShootCoroutine);
            burstShootCoroutine = null;
        }

        burstShootCoroutine = StartCoroutine(AttackSequenceRoutine());
    }

    /// <summary>
    /// 공격 1회 시퀀스.
    /// 
    /// 3연발 없음:
    /// - 1번만 발사.
    /// 
    /// 3연발 있음:
    /// - FinalBurstCount만큼 반복 발사.
    /// 
    /// 산탄 있음:
    /// - 각 반복마다 FinalMultiShotCount만큼 퍼져서 발사.
    /// </summary>
    private IEnumerator AttackSequenceRoutine()
    {
        isAttackSequenceRunning = true;

        int burstCount = FinalBurstCount;
        float interval = Mathf.Max(0.03f, burstInterval);

        for (int i = 0; i < burstCount; i++)
        {
            if (shotUseOrbitProjectile)
            {
                CancelNormalAttackBecauseOrbit();
                isAttackSequenceRunning = false;
                burstShootCoroutine = null;
                yield break;
            }

            PlayAttackAnimation();
            ShootOneAttackStep();

            if (i < burstCount - 1)
                yield return new WaitForSeconds(interval);
        }

        isAttackSequenceRunning = false;
        burstShootCoroutine = null;
    }

    /// <summary>
    /// 3연발의 각 1타마다 실제 발사되는 함수.
    /// 
    /// 산탄이 있으면 여러 발.
    /// 산탄이 없으면 단발.
    /// </summary>
    private void ShootOneAttackStep()
    {
        if (multiShotEnabled)
        {
            ShootMultiShotByCount(FinalMultiShotCount, FinalMultiShotSpreadAngle);
            return;
        }

        ShootSingleWithDamageMultiplier(1f);
    }

    /// <summary>
    /// 단발 발사.
    /// 차지샷 데미지 배율도 여기서 같이 적용한다.
    /// </summary>
    private void ShootSingleWithDamageMultiplier(float extraMultiplier)
    {
        if (IsDie) return;
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;

        Vector2 shotDirection = lockedAttackAimDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = animDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = Vector2.down;

        shotDirection.Normalize();

        float baseAngle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, baseAngle);

        Vector3 spawnPos = gunTip.position + (Vector3)(shotDirection * bulletSpawnOffset);

        GameObject bullet = Instantiate(curProjectilePrefab, spawnPos, bulletRotation);
        ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        float finalDamage = GetFinalDamage() * extraMultiplier * GetChargeDamageMultiplierIfReady();
        ApplyProjectileAugmentToBullet(bulletScript, finalDamage);
    }

    /// <summary>
    /// 지정한 개수와 각도로 산탄 발사.
    /// 
    /// 3연발 + 산탄이면
    /// AttackSequenceRoutine에서 이 함수가 여러 번 호출된다.
    /// </summary>
    private void ShootMultiShotByCount(int bulletCount, float spreadAngle)
    {
        if (IsDie) return;
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;

        bulletCount = Mathf.Max(1, bulletCount);

        Vector2 shotDirection = lockedAttackAimDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = animDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = Vector2.down;

        shotDirection.Normalize();

        float baseAngle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
        float startAngle = -spreadAngle * (bulletCount - 1) * 0.5f;

        for (int i = 0; i < bulletCount; i++)
        {
            float addAngle = startAngle + spreadAngle * i;
            float finalAngle = baseAngle + addAngle;

            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, finalAngle);

            Vector2 finalDir = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            ).normalized;

            Vector3 spawnPos = gunTip.position + (Vector3)(finalDir * bulletSpawnOffset);

            GameObject bullet = Instantiate(curProjectilePrefab, spawnPos, bulletRotation);
            ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

            if (bulletScript != null)
            {
                bulletScript.SetOwner(gameObject);
            }

            float finalDamage = GetFinalDamage() * GetChargeDamageMultiplierIfReady();
            ApplyProjectileAugmentToBullet(bulletScript, finalDamage);
        }
    }

    /// <summary>
    /// 차지샷이 활성화되어 있으면 차지샷 데미지 배율을 반환한다.
    /// </summary>
    private float GetChargeDamageMultiplierIfReady()
    {
        if (!chargeShotEnabled)
            return 1f;

        return chargeShotDamageMultiplier;
    }

    /// <summary>
    /// 현재 공격 간격 반환.
    /// 
    /// 자동연사는 패시브가 아니므로 별도 보정값을 적용하지 않는다.
    /// 공격 간격은 항상 AttackPerSecond 기준이다.
    /// 
    /// 예:
    /// AttackPerSecond = 1이면 1초마다 공격
    /// AttackPerSecond = 2이면 0.5초마다 공격
    /// AttackPerSecond = 4이면 0.25초마다 공격
    /// </summary>
    private float GetCurrentAttackInterval()
    {
        float interval = 1f / Mathf.Max(0.1f, AttackPerSecond);
        return Mathf.Max(0.01f, interval);
    }

    //단추(기본공격) 발사

    #region 공격 발사

    private void ShootSingle()
    {
        if (IsDie) return;
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;

        Vector2 shotDirection = lockedAttackAimDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = animDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = Vector2.down;

        shotDirection.Normalize();

        float baseAngle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, baseAngle);

        // ------------------------------------------------------------
        // 총알을 gunTip 위치에서 바로 생성하지 않고,
        // 발사 방향 앞으로 조금 밀어서 생성
        // 이유:
        // 1. 플레이어 몸 안에서 생성되는 현상 방지
        // 2. 생성 직후 자기 자신과 겹쳐보이는 문제 방지
        // ------------------------------------------------------------
        Vector3 spawnPos = gunTip.position + (Vector3)(shotDirection * bulletSpawnOffset);

        GameObject bullet = Instantiate(curProjectilePrefab, spawnPos, bulletRotation);
        ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        ApplyProjectileAugmentToBullet(bulletScript, GetFinalDamage());
    }

    private void ShootMultiShot()
    {
        if (IsDie) return;
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;

        int currentBulletCount = Mathf.Max(1, 1 + shotProjectileCountAdd);
        float currentSpreadAngle = shotSpreadAngle;

        Vector2 shotDirection = lockedAttackAimDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = animDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = Vector2.down;

        shotDirection.Normalize();

        float baseAngle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
        float startAngle = -currentSpreadAngle * (currentBulletCount - 1) * 0.5f;

        for (int i = 0; i < currentBulletCount; i++)
        {
            float addAngle = startAngle + (currentSpreadAngle * i);
            float finalAngle = baseAngle + addAngle;

            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, finalAngle);

            Vector2 finalDir = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            ).normalized;

            Vector3 spawnPos = gunTip.position + (Vector3)(finalDir * bulletSpawnOffset);

            GameObject bullet = Instantiate(curProjectilePrefab, spawnPos, bulletRotation);
            ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

            if (bulletScript != null)
            {
                bulletScript.SetOwner(gameObject);
            }

            ApplyProjectileAugmentToBullet(bulletScript, GetFinalDamage());
        }
    }

    #endregion

    /// <summary>
    /// 주위탄을 보유한 상태에서는 일반 입력 공격을 완전히 막는다.
    /// 
    /// 역할:
    /// 1. 마우스 입력 상태 제거
    /// 2. 단발 공격 요청 제거
    /// 3. 차지 상태 제거
    /// 4. 진행 중인 3연발 코루틴 중단
    /// 5. 공격 애니메이션 트리거 제거
    /// 
    /// 이유:
    /// 주위탄은 일반 공격 대체형이므로,
    /// 주위탄 선택 후에는 마우스 클릭으로 ShootSingle / ShootMultiShot 이 실행되면 안 된다.
    /// </summary>
    private void CancelNormalAttackBecauseOrbit()
    {
        EndChargeShot();
        isFireInput = false;
        requestSingleShot = false;
        firedThisPress = false;
        chargeStartTime = -1f;

        if (burstShootCoroutine != null)
        {
            StopCoroutine(burstShootCoroutine);
            burstShootCoroutine = null;
        }

        if (bodyAnimator != null)
        {
            bodyAnimator.ResetTrigger(AttackHash);
        }

        if (playerState == PlayerState.Attack)
        {
            playerState = inputDirection.sqrMagnitude > 0.01f
                ? PlayerState.Walk
                : PlayerState.Idle;

            SyncLocomotionState();
        }
    }

    private void ApplyProjectileAugmentToBullet(ButtonSpawn bulletScript, float damage)
    {
        if (bulletScript == null) return;

        bulletScript.SetDamage(damage);
        bulletScript.SetProjectileLifeMultiplier(shotProjectileLifeMultiplier);

        bulletScript.SetHoming(FinalHomingStrength, FinalTrajectoryDuration);
        bulletScript.SetBounce(FinalBounceCount);
        bulletScript.SetPierce(FinalPierceCount);

        bulletScript.SetExplosion(FinalExplosionRadius, FinalExplosionDamageMultiplier);
        bulletScript.SetChain(FinalChainCount, FinalChainRange);
        bulletScript.SetDot(FinalDotDamagePerSecond, FinalDotDuration);
    }

    public void SetControl(bool value)
    {
        EndChargeShot();
        canControl = value;

        if (!canControl)
        {
            isDashing = false;
            dashEndTime = -999f;

            inputDirection = Vector2.zero;
            isFireInput = false;
            rb.linearVelocity = Vector2.zero;
            playerState = PlayerState.Idle;

            if (bodyAnimator != null)
            {
                bodyAnimator.SetBool(IsMoveHash, false);
                bodyAnimator.ResetTrigger(AttackHash);
            }
        }
        else
        {
            SyncLocomotionState();
        }
    }

    private void OnMove(InputValue movementValue)
    {
        if (!IsGameplayScene())
        {
            inputDirection = Vector2.zero;
            SyncLocomotionState();
            return;
        }

        if (IsDie)
        {
            inputDirection = Vector2.zero;
            SyncLocomotionState();
            return;
        }

        if (!canControl)
        {
            inputDirection = Vector2.zero;
            SyncLocomotionState();
            return;
        }

        inputDirection = movementValue.Get<Vector2>();

        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = inputDirection.normalized;
        }

        // 입력 들어오자마자 애니메이터와 상태 동기화
        SyncLocomotionState();
    }

    private void OnAttack(InputValue value)
    {
        if (shotUseOrbitProjectile)
        {
            CancelNormalAttackBecauseOrbit();
            return;
        }

        if (!IsGameplayScene())
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            EndChargeShot();
            return;
        }

        if (IsDie)
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            EndChargeShot();
            return;
        }

        if (!canControl)
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            EndChargeShot();
            return;
        }

        bool pressed = value.isPressed;

        // 마우스를 처음 눌렀을 때
        if (pressed && !isFireInput)
        {
            requestSingleShot = true;
            firedThisPress = false;

            if (FinalChargeShotTime > 0f)
            {
                BeginChargeShot();
            }
            else
            {
                chargeStartTime = -1f;
            }
        }

        // 마우스를 뗐을 때
        if (!pressed)
        {
            firedThisPress = false;

            // 차지 완료 전에 떼면 취소
            if (FinalChargeShotTime > 0f && !IsChargeReady())
            {
                requestSingleShot = false;
                EndChargeShot();
            }
        }

        isFireInput = pressed;
    }

    private bool CanStartAttackNow()
    {
        if (!IsGameplayScene()) return false;
        if (IsDie) return false;
        if (!canControl) return false;
        if (isPointerOverUIThisFrame) return false;
        if (isDashing) return false;
        if (playerState == PlayerState.Hit) return false;
        if (playerState == PlayerState.Death) return false;

        return true;
    }

    private void PlayAttackAnimation()
    {
        playerState = PlayerState.Attack;

        lockedAttackAimDirection = aimDirection.sqrMagnitude > 0.0001f
            ? aimDirection
            : animDirection;

        animDirection = lockedAttackAimDirection;
        ApplyBlendTreeDirection(animDirection);

        if (bodyAnimator != null)
        {
            if (bodyAnimator.GetBool(IsMoveHash))
            {
                bodyAnimator.speed = 1.0f;
            }
            else
            {
                bodyAnimator.speed = AttackPerSecond;
            }
            
            bodyAnimator.ResetTrigger(HitHash);
            bodyAnimator.ResetTrigger(AttackHash);
            bodyAnimator.SetTrigger(AttackHash);
        }
    }

    private void ReleaseAttackState()
    {
        if (playerState != PlayerState.Attack)
            return;
        if (bodyAnimator != null)
        {
            bodyAnimator.speed = 1f;
            bodyAnimator.ResetTrigger(AttackHash);
        }

        // 공격 끝난 직후 현재 입력 기준으로 상태 복귀
        playerState = inputDirection.sqrMagnitude > 0.01f
            ? PlayerState.Walk
            : PlayerState.Idle;

        // 다시 마우스 방향으로 BlendTree 방향 갱신
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            animDirection = aimDirection;
            ApplyBlendTreeDirection(animDirection);
        }

        // 이동 상태 재동기화
        SyncLocomotionState();
    }
    
    
    #region 차지 처리
    private void SetChargeAnimation(bool value)
    {
        if (bodyAnimator == null)
            return;

        if (isChargingShot == value)
            return;

        isChargingShot = value;
        bodyAnimator.SetBool(IsChargeHash, value);

        if (value)
        {
            playerState = PlayerState.Attack;
        }
    }
    
    private void BeginChargeShot()
    {
        if (FinalChargeShotTime <= 0f)
            return;

        if (chargeStartTime < 0f)
            chargeStartTime = Time.time;

        lockedAttackAimDirection = aimDirection.sqrMagnitude > 0.0001f
            ? aimDirection.normalized
            : animDirection.sqrMagnitude > 0.0001f
                ? animDirection.normalized
                : Vector2.down;

        animDirection = lockedAttackAimDirection;
        ApplyBlendTreeDirection(animDirection);

        SetChargeAnimation(true);
    }
    
    private void EndChargeShot()
    {
        chargeStartTime = -1f;
        SetChargeAnimation(false);
    }
    
    private bool IsChargeReady()
    {
        if (FinalChargeShotTime <= 0f)
            return true;

        if (chargeStartTime < 0f)
            return false;

        return Time.time >= chargeStartTime + FinalChargeShotTime;
    }
    #endregion
    
    
    #region 피격 처리

    public void OnDamage(float damage)
    {
        if (IsDie) return;

        if (Time.time < invincibleUntilTime)
            return;

        // 보호막 먼저 소모
        if (currentShieldCount > 0)
        {
            currentShieldCount--;
            invincibleUntilTime = Time.time + 0.1f;

            // 보호막이 사라졌으니 시각 효과 갱신
            RefreshShieldVisual();
            return;
        }

        float incomingDamage = Mathf.Max(0f, damage);

        if (shieldActive)
        {
            incomingDamage = Mathf.CeilToInt(incomingDamage * 0.5f);
        }
        // 치명적 피해 극복
        if (Hp - incomingDamage <= 0f && specialCheatDeath && !specialCheatDeathUsed)
        {
            specialCheatDeathUsed = true;
            Hp = Mathf.Max(1f, specialCheatDeathHp);
            invincibleUntilTime = Time.time + specialCheatDeathInvincibleDuration;
            return;
        }

        Hp -= incomingDamage;
        

        if (Hp <= 0)
        {
            Death();
            return;
        }

        EnterHitState();
    }

    #endregion

    private void EnterHitState()
    {
        EndChargeShot();
        if (IsDie) return;

        IsHitAnimating = true;
        playerState = PlayerState.Hit;

        isDashing = false;
        dashEndTime = -999f;
        rb.linearVelocity = Vector2.zero;

        animDirection = aimDirection.sqrMagnitude > 0.0001f
            ? aimDirection
            : animDirection;

        ApplyBlendTreeDirection(animDirection);

        if (bodyAnimator != null)
        {
            bodyAnimator.speed = 1f;
            bodyAnimator.ResetTrigger(AttackHash);
            bodyAnimator.SetTrigger(HitHash);
        }

        UpdateAnimatorLocomotion();
    }
    
    #region 최종 데미지 계산

    public float GetFinalDamage()
    {
        float baseDamage = weaponDamage + itemDamage;

        float subSkillAppliedDamage = baseDamage * shotDamageMultiplier;
        float passiveAppliedDamage = subSkillAppliedDamage * passiveDamageMultiplier;

        float hpBonusMultiplier = 1f;

        if (specialHpToDamagePercentPer10Hp > 0f)
        {
            hpBonusMultiplier += (maxHp / 10f) * specialHpToDamagePercentPer10Hp;
        }

        float finalDamage = passiveAppliedDamage * hpBonusMultiplier;

        // Round를 제거해야 낮은 데미지 구간에서도
        // 패시브 / 스페셜 데미지 증가가 체감된다.
        return Mathf.Max(0f, finalDamage * attackMultiplier);
    }

    #endregion

    private void OnDash(InputValue value)
    {
        if (!value.isPressed) return;
        if (!IsGameplayScene()) return;
        if (IsDie) return;
        if (!canControl) return;
        if (Time.timeScale <= 0f) return;
        if (isDashing) return;
        if (IsDashOnCooldown()) return;

        StartDash();
    }

    private void StartDash()
    {
        if (isKnockback) //* 넉백 몬스터 공격
            return;

        Vector2 dir = Vector2.zero;

        // 1순위 : 현재 이동 입력 방향
        if (inputDirection.sqrMagnitude > 0.0001f)
        {
            dir = inputDirection.normalized;
        }
        // 2순위 : 마지막 이동 방향
        else if (lastMoveDirection.sqrMagnitude > 0.0001f)
        {
            dir = lastMoveDirection.normalized;
        }
        else
        {
            return; // 이동 방향 정보가 없으면 대쉬 안 함
        }

        dashDirection = dir;
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        lastDashTime = Time.time;

        // 대쉬 시작 즉시 속도 반영
        rb.linearVelocity = dashDirection * DashSpeed;

        // 필요 시 공격 상태 끊기
        playerState = PlayerState.Idle;
    }

    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        playerState = inputDirection.sqrMagnitude > 0.01f
            ? PlayerState.Walk
            : PlayerState.Idle;
    }

    public void Death()
    {
        EndChargeShot();
        if (IsDie) return;

        IsDie = true;
        playerState = PlayerState.Death;
        Cursor.visible = true;

        inputDirection = Vector2.zero;
        isFireInput = false;

        isDashing = false;
        dashEndTime = -999f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        canControl = false;

        animDirection = aimDirection.sqrMagnitude > 0.0001f
            ? aimDirection
            : animDirection;

        ApplyBlendTreeDirection(animDirection);

        if (bodyAnimator != null)
        {
            bodyAnimator.speed = 1f;
            bodyAnimator.SetBool(IsMoveHash, false);
            bodyAnimator.ResetTrigger(AttackHash);
            bodyAnimator.SetBool(IsDeathHash, true);
        }

        StopAllCoroutines();
        deathUICoroutine = null;

        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(false);
        }

        ClearOrbitProjectiles();
    }

    /// <summary>
    /// 주위탄 재생성.
    /// 
    /// 최종 규칙:
    /// - 주위탄 기본 개수는 Lv1 기준.
    /// - 주위탄 Lv2 이상은 데미지/반경/속도/타격간격만 강화.
    /// - 3연발/산탄 개수는 주위탄 편대 개수에 반영.
    /// 
    /// 예:
    /// 주위탄 4개 + 3연발 Lv2 4발 = 4 * 4 = 16개
    /// 주위탄 4개 + 산탄 6발 = 4 * 6 = 24개
    /// 주위탄 4개 + 3연발 4발 + 산탄 6발 = 4 * 4 * 6 = 96개지만 maxOrbitProjectileTotal로 제한
    /// </summary>


    //무기 및 아이템
    public void ApplyItem(ItemData item)
    {
        if (item == null) return;

        // 장착 가능 여부 체크
        if (equippedItems.Count >= MAX_ITEM_COUNT)
        {
            Debug.Log("아이템 장착 불가: 최대 5개");
            return;
        }

        // 리스트에 추가
        equippedItems.Add(item);

        // 전체 스탯 재계산
        //RecalculateItemStats();
        RebuildPlayerStats();
    }

    public void UnequipItem(int index)
    {
        if (index < 0 || index >= equippedItems.Count)
            return;

        ItemData item = equippedItems[index];
        if (item == null)
            return;

        equippedItems.RemoveAt(index);
        //
        RebuildPlayerStats();

        
        if (CurItemUI.Instance != null)
        {
            CurItemUI.Instance.SetItems(equippedItems);
        }

        //
        ItemUIManager.Instance?.HideUIItemInfo();
        if (itemPickupPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.right * 1.0f;

            GameObject drop = Instantiate(itemPickupPrefab, spawnPos, Quaternion.identity);

            ItemPickup pickup = drop.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.SetItemData(item);
            }
        }


        RebuildPlayerStats();

        Hp = Mathf.Min(Hp, MaxHp);
    }
    public bool EquipItem(ItemData itemData)
    {
        if (equippedItems.Count >= 5)
        {
            Debug.Log("아이템 최대치");
            return false;
        }

        equippedItems.Add(itemData);

        //RecalculateItemStats();
        RebuildPlayerStats();
        return true;
    }
    private void RecalculateItemStats()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ResetCooldownReduction();//*0510
        }

        // 초기화 (아이템 영향 제거)
        itemDamage = 0f;
        moveSpeed = baseMoveSpeed;
        attackPerSecond = baseAttackPerSecond;
        maxHp = baseMaxHp;

        // 모든 장착 아이템 적용
        foreach (var item in equippedItems) //조건부 아이템
        {
            if (item == null) continue;

            // 기본 스탯은 항상 더함
            itemDamage += item.damage;
            moveSpeed += item.moveSpeed;
            attackPerSecond += item.bulletRate;
            maxHp += item.hp;

            // 특수 효과 적용
            item.ApplyEffectStat(this);
        }

        // 체력 보정
        Hp = Mathf.Clamp(Hp, 0, maxHp);

        UpdateAnimatorPlaybackSpeed();
        SyncLocomotionState();
    }

    public void ApplyWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;
        if (weapon == null)
        {
            weaponDamage = 0f;
            curProjectilePrefab = null;
            return;
        }

        weaponDamage = weapon.damage;
        curProjectilePrefab = weapon.projectilePrefab;

        if (CurWeaponUI.Instance != null)
        {
            CurWeaponUI.Instance.SetWeapon(weapon);
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        ApplyWeapon(newWeapon);
    }
    public List<ItemData> GetEquippedItems()
    {
        return equippedItems;
    }
    
    private void RefreshOrbitProjectiles()
    {
        ClearOrbitProjectiles();

        if (!shotUseOrbitProjectile) return;
        if (shotOrbitProjectileCount <= 0) return;
        if (orbitProjectilePrefab == null) return;

        Transform orbitOwner = playerBody != null ? playerBody : transform;

        int rowCount = Mathf.Max(1, shotOrbitProjectileCount);
        int columnCount = GetOrbitFormationColumnCount(rowCount);

        for (int row = 0; row < rowCount; row++)
        {
            float baseAngle = orbitStartAngleOffset + ((360f / rowCount) * row);

            for (int column = 0; column < columnCount; column++)
            {
                Vector2 formationOffset = GetOrbitFormationOffset(column, columnCount);

                GameObject orbitObj = Instantiate(
                    orbitProjectilePrefab,
                    orbitOwner.position,
                    Quaternion.identity,
                    transform
                );

                OrbitProjectile orbit = orbitObj.GetComponent<OrbitProjectile>();
                if (orbit == null)
                {
                    Destroy(orbitObj);
                    continue;
                }

                float orbitDamage = GetFinalDamage() * shotOrbitDamageMultiplier;

                float orbitAttackSpeedMultiplier = Mathf.Max(0.1f, 1f + passiveAttackSpeedPercent);
                float finalOrbitAngularSpeed = shotOrbitAngularSpeed * orbitAttackSpeedMultiplier;
                float finalOrbitHitInterval = shotOrbitHitInterval / orbitAttackSpeedMultiplier;

                orbit.Initialize(
                    orbitOwner,
                    baseAngle,
                    shotOrbitRadius,
                    finalOrbitAngularSpeed,
                    orbitDamage,
                    finalOrbitHitInterval,
                    shotOrbitLifetime,
                    formationOffset
                );

                orbit.SetOwner(gameObject);

                orbit.SetExplosion(FinalExplosionRadius, FinalExplosionDamageMultiplier);
                orbit.SetDot(FinalDotDamagePerSecond, FinalDotDuration);
                
                
                activeOrbitProjectiles.Add(orbit);
            }
        }
    }

    /// <summary>
    /// 주위탄 한 줄에 몇 개를 배치할지 계산한다.
    /// 
    /// 3연발과 산탄이 모두 있으면 둘을 곱한다.
    /// 단, 최적화를 위해 maxOrbitProjectileTotal을 넘지 않도록 제한한다.
    /// </summary>
    private int GetOrbitFormationColumnCount(int rowCount)
    {
        int rawColumnCount = Mathf.Max(1, FinalOrbitColumnCount);

        if (maxOrbitProjectileTotal <= 0)
            return rawColumnCount;

        int maxColumnCount = Mathf.Max(1, maxOrbitProjectileTotal / Mathf.Max(1, rowCount));

        return Mathf.Min(rawColumnCount, maxColumnCount);
    }

    /// <summary>
    /// 같은 주위탄 기본 위치 안에서 좌/중/우처럼 벌어지는 위치를 계산한다.
    /// </summary>
    private Vector2 GetOrbitFormationOffset(int columnIndex, int columnCount)
    {
        if (columnCount <= 1)
            return Vector2.zero;

        float startX = -orbitFormationSpacing * (columnCount - 1) * 0.5f;
        float x = startX + orbitFormationSpacing * columnIndex;

        return new Vector2(x, 0f);
    }

    private void ClearOrbitProjectiles()
    {
        for (int i = 0; i < activeOrbitProjectiles.Count; i++)
        {
            if (activeOrbitProjectiles[i] != null)
            {
                Destroy(activeOrbitProjectiles[i].gameObject);
            }
        }

        activeOrbitProjectiles.Clear();
    }

    public void EndDeathAnimationEvent()
    {
        if (!IsDie) return;
        if (PlayerUIManager.Instance == null) return;

        // 애니메이션 이벤트가 중복으로 들어와도 한 번만 실행
        if (deathUICoroutine != null) return;

        deathUICoroutine = StartCoroutine(ShowDeathUI());
    }

    private IEnumerator ShowDeathUI()
    {
        if (deathUIShowDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(deathUIShowDelay);
        }

        // 딜레이 도중 재시작되었거나 살아났으면 UI 띄우지 않음
        if (!IsDie)
        {
            deathUICoroutine = null;
            yield break;
        }

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowPlayerDyingUI();
        }

        deathUICoroutine = null;
    }

    //증강 - 적 처치시 체력회복

    #region 회복 / 흡혈

    public void OnHitEnemy(float dealtDamage)
    {
        if (IsDie) return;
        if (passiveLifeStealPercent <= 0f) return;
        if (dealtDamage <= 0f) return;

        float healAmount = dealtDamage * passiveLifeStealPercent;

        Heal(healAmount, true);

        Debug.Log(
            "[흡혈] 준 데미지: " + dealtDamage.ToString("F2") +
            " / 흡혈 비율: " + (passiveLifeStealPercent * 100f).ToString("F1") + "%" +
            " / 회복량: " + healAmount.ToString("F2")
        );
    }

    /// <summary>
    /// 기존 코드 호환용.
    /// 혹시 다른 곳에서 OnHitEnemy()를 아직 호출하고 있다면 에러 방지.
    /// 가능하면 새 코드에서는 OnHitEnemy(float dealtDamage)를 사용할 것.
    /// </summary>
    public void OnHitEnemy()
    {
        OnHitEnemy(GetFinalDamage());
    }


    public void OnKillEnemy()
    {
        if (IsDie) return;
        if (specialHealOnKill <= 0f) return;

        Heal(specialHealOnKill, true);
        
    }

    public void Heal(float value, bool triggerAutoAttack = true)
    {
        if (IsDie) return;
        if (value <= 0f) return;

        float beforeHp = Hp;

        // float 회복량 그대로 적용
        Hp += value;

        float actualHealed = Hp - beforeHp;

        if (actualHealed <= 0f) return;

        Debug.Log(
            "회복량: " + actualHealed.ToString("F2") +
            " / 현재 체력: " + Hp.ToString("F2") +
            " / 최대 체력: " + MaxHp.ToString("F2")
        );

        if (triggerAutoAttack &&
            specialHealToAutoAttackThreshold > 0f &&
            specialHealToAutoAttackDamageMultiplier > 0f)
        {
            healAccumulatorForAutoAttack += actualHealed;

            while (healAccumulatorForAutoAttack >= specialHealToAutoAttackThreshold)
            {
                healAccumulatorForAutoAttack -= specialHealToAutoAttackThreshold;
                SpawnHealAutoAttack();
            }
        }
    }

    private void SpawnHealAutoAttack()
    {
        if (IsDie) return;
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;

        Vector2 shotDirection = aimDirection.sqrMagnitude > 0.0001f
            ? aimDirection
            : animDirection;

        if (shotDirection.sqrMagnitude <= 0.0001f)
            shotDirection = Vector2.down;

        shotDirection.Normalize();

        float baseAngle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, baseAngle);

        Vector3 spawnPos = gunTip.position + (Vector3)(shotDirection * bulletSpawnOffset);

        GameObject bullet = Instantiate(curProjectilePrefab, spawnPos, bulletRotation);
        ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        float autoAttackDamage = GetFinalDamage() * specialHealToAutoAttackDamageMultiplier;
        ApplyProjectileAugmentToBullet(bulletScript, autoAttackDamage);
    }

    #endregion

    #region 특수형 지속 시간 처리

    private void TickSpecialRuntime()
    {
        // 초당 체력 회복
        if (specialHpRegenPerSecond > 0f)
        {
            Heal(specialHpRegenPerSecond * Time.deltaTime, true);
        }

        // 보호막 충전
        if (specialShieldInterval > 0f && specialShieldMaxCount > 0)
        {
            if (Time.time >= nextShieldChargeTime)
            {
                currentShieldCount = Mathf.Min(currentShieldCount + 1, specialShieldMaxCount);
                nextShieldChargeTime = Time.time + specialShieldInterval;

                // 보호막이 충전됐으니 시각 효과 갱신
                RefreshShieldVisual();
            }
        }
    }

    #endregion

    //UI-Esc일시정지
    private void OnPause(InputValue value)
    {
        Debug.Log($"PlayerEsc [RAW INPUT] isPressed={value.isPressed}");

        if (!value.isPressed)
        {
            Debug.Log("PlayerEsc [IGNORED: not pressed]");
            return;
        }
        if (!IsGameplayScene())
        {
            Debug.Log("PlayerEsc [IGNORED: not gameplay]");
            return;
        }
        if (IsDie)
        {
            Debug.Log("PlayerEsc [IGNORED: dead]");
            return;
        }
        Debug.Log("PlayerEsc [CALL HandleEscape]");
        PlayerUIManager.Instance?.HandleEscape();
    }

    public void SetPause(bool isPaused)
    {
        Cursor.visible = isPaused;

        if (crosshairTransform != null)
            crosshairTransform.gameObject.SetActive(!isPaused);
    }

    //증강변수 초기화

    #region 증강 재빌드

    public void RebuildPlayerStats()
    {
        float beforeMaxHp = maxHp;
        float beforeHp = Hp;
        hasDisplayTicket = false;

        // 1. 기본값 복원
        maxHp = baseMaxHp;
        moveSpeed = baseMoveSpeed;
        AttackPerSecond = baseAttackPerSecond;

        weaponDamage = 0f;
        itemDamage = 0f;
        curProjectilePrefab = null;

        // 2. 장비 재적용
        if (currentWeapon == null && basicWeapon != null)
        {
            currentWeapon = basicWeapon;
        }

        if (currentWeapon != null)
        {
            ApplyWeapon(currentWeapon);
        }
        // 아이템 전체 재적용
        RecalculateItemStats();
        

        // 3. 증강 런타임 초기화
        ResetAugmentRuntimeValues();

        // 4. 보유 증강 재적용
        if (AugmentRunManager.Instance != null)
        {
            AugmentSlotData[] slots = AugmentRunManager.Instance.OwnedSlots;
            int slotCount = AugmentRunManager.Instance.CurrentSlotCount;

            for (int i = 0; i < slotCount; i++)
            {
                if (slots[i] == null) continue;
                if (!slots[i].isOccupied) continue;
                if (slots[i].augmentData == null) continue;

                ApplyOwnedAugmentSlot(slots[i]);
            }
        }

        // 5. 패시브 / 특수 최종 반영
        maxHp += passiveMaxHpAdd;
        maxHp = Mathf.Max(1, maxHp);

        moveSpeed = (moveSpeed * specialMoveSpeedMultiplier) + specialMoveSpeedAdd;
        moveSpeed = Mathf.Max(0f, moveSpeed);

        AttackPerSecond = Mathf.Max(
            0.1f,
            attackPerSecond * (1f + passiveAttackSpeedPercent + shotAttackSpeedBonusPercent)
        );

        transform.localScale = defaultPlayerScale * specialCharacterScaleMultiplier;
        ApplyObstacleCollisionRule(specialIgnoreObstacleCollision);

        if (specialShieldInterval > 0f && specialShieldMaxCount > 0)
        {
            currentShieldCount = 1;
            nextShieldChargeTime = Time.time + specialShieldInterval;
        }
        else
        {
            currentShieldCount = 0;
            nextShieldChargeTime = -1f;
        }
        
        RefreshShieldVisual();
        
        float maxHpIncrease = maxHp - beforeMaxHp;

        if (maxHpIncrease > 0)
        {
            Hp = Mathf.Clamp(beforeHp + maxHpIncrease, 0, maxHp);
        }
        else
        {
            Hp = Mathf.Clamp(beforeHp, 0, maxHp);
        }

        RefreshOrbitProjectiles();
        if (shotUseOrbitProjectile)
        {
            CancelNormalAttackBecauseOrbit();
        }
        

        maxHp = Mathf.Max(1, maxHp);//*

        // HP는 유지, 단 최대값 초과만 방지
        Hp = Mathf.Min(beforeHp, maxHp);
    }


    private void ResetAugmentRuntimeValues()
    {
        // 서브 스킬 초기값
        currentShotAugment = null;
        currentTrajectoryAugment = null;
        currentEffectAugment = null;

        currentShotLevel = 0;
        currentTrajectoryLevel = 0;
        currentEffectLevel = 0;

        shotProjectileCountAdd = 0;
        shotDamageMultiplier = 1f;
        shotSpreadAngle = 0f;
        shotChargeTime = 0f;
        shotFireMode = AugmentationSystem.ShotFireMode.Single;
        shotBurstInterval = 0f;
        shotAttackSpeedBonusPercent = 0f;
        shotProjectileLifeMultiplier = 1f;

        // 3연발 초기화
        burstEnabled = false;
        burstProjectileCount = 1;
        burstInterval = 0.12f;

// 산탄 초기화
        multiShotEnabled = false;
        multiShotProjectileCount = 1;
        multiShotSpreadAngle = 0f;

// 차지샷 초기화
        chargeShotEnabled = false;
        chargeShotTime = 0f;
        chargeShotDamageMultiplier = 1f;

        trajectoryHomingStrength = 0f;
        trajectoryDuration = 0f;
        trajectoryBounceCount = 0;
        trajectoryPierceCount = 0;

        effectExplosionRadius = 0f;
        effectExplosionDamageMultiplier = 1f;
        effectChainCount = 0;
        effectChainRange = 0f;
        effectDotDamagePerSecond = 0f;
        effectDotDuration = 0f;

        shotUseOrbitProjectile = false;
        shotOrbitProjectileCount = 0;
        shotOrbitRadius = 1.5f;
        shotOrbitAngularSpeed = 180f;
        shotOrbitHitInterval = 0.2f;
        shotOrbitDamageMultiplier = 1f;
        shotOrbitLifetime = 0f;

        ClearOrbitProjectiles();

        // 패시브 초기값
        passiveDamageMultiplier = 1f;
        passiveAttackSpeedPercent = 0f;
        passiveMaxHpAdd = 0;
        passiveLifeStealPercent = 0f;
        
        // 특수형 초기값
        specialMoveSpeedMultiplier = 1f;
        specialMoveSpeedAdd = 0f;
        specialCharacterScaleMultiplier = 1f;
        specialIgnoreObstacleCollision = false;

        specialCheatDeath = false;
        specialCheatDeathUsed = false;
        specialCheatDeathHp = 1;
        specialCheatDeathInvincibleDuration = 1f;

        specialHpToDamagePercentPer10Hp = 0f;
        specialHealOnKill = 0f;
        specialHpRegenPerSecond = 0f;
        specialHealToAutoAttackThreshold = 0f;
        specialHealToAutoAttackDamageMultiplier = 0f;

        specialShieldInterval = 0f;
        specialShieldMaxCount = 0;
        currentShieldCount = 0;

        nextShieldChargeTime = -1f;
        invincibleUntilTime = -1f;
        hpRegenAccumulator = 0f;
        healAccumulatorForAutoAttack = 0f;

        requestSingleShot = false;
        firedThisPress = false;

        transform.localScale = defaultPlayerScale;
        ApplyObstacleCollisionRule(false);
    }

    private void ApplyOwnedAugmentSlot(AugmentSlotData slot)
    {
        if (slot == null) return;
        if (!slot.isOccupied) return;
        if (slot.augmentData == null) return;

        AugmentationSystem aug = slot.augmentData;

        switch (aug.category)
        {
            case AugmentationSystem.AugmentCategory.SubSkill:
                ApplySubSkillAugment(aug, slot.currentLevel);
                break;

            case AugmentationSystem.AugmentCategory.Passive:
                ApplyPassiveAugment(aug, slot.stackCount);
                break;

            case AugmentationSystem.AugmentCategory.Special:
                ApplySpecialAugment(aug, slot.currentLevel);
                break;
        }
    }

    /// <summary>
    /// 서브 스킬형 증강을 플레이어 런타임 값에 적용한다.
    /// 
    /// 핵심:
    /// - Shot은 일반 발사형과 주위탄형을 분리한다.
    /// - Trajectory는 조합 가능하도록 누적한다.
    /// - Effect는 조합 가능하도록 누적한다.
    /// 
    /// 최적화:
    /// - 매 프레임 실행되는 함수가 아님.
    /// - 증강 선택 / 스탯 재계산 때만 실행됨.
    /// - 따라서 가독성과 안정성을 우선한다.
    /// </summary>
    private void ApplySubSkillAugment(AugmentationSystem aug, int level)
    {
        if (aug == null) return;

        AugmentationSystem.SubSkillLevelData data = aug.GetSubSkillLevelData(level);
        if (data == null) return;

        switch (aug.subSkillType)
        {
            case AugmentationSystem.SubSkillType.Shot:
            {
                ApplyShotSubSkillAugment(aug, data, level);
                break;
            }

            case AugmentationSystem.SubSkillType.Trajectory:
            {
                ApplyTrajectorySubSkillAugment(aug, data, level);
                break;
            }

            case AugmentationSystem.SubSkillType.Effect:
            {
                ApplyEffectSubSkillAugment(aug, data, level);
                break;
            }
        }
    }

    /// <summary>
    /// 발사형 서브 스킬 적용.
    /// 
    /// 기존 구조:
    /// - shotFireMode 하나로 3연발/산탄/차지/주위탄을 덮어씀.
    /// 
    /// 변경 구조:
    /// - 3연발, 산탄, 차지샷, 주위탄을 각각 별도 modifier로 저장.
    /// - 그래서 3연발 + 산탄 + 차지샷 조합이 가능해진다.
    /// </summary>
    private void ApplyShotSubSkillAugment(AugmentationSystem aug, AugmentationSystem.SubSkillLevelData data, int level)
    {
        if (aug == null) return;
        if (data == null) return;

        switch (aug.shotModifierType)
        {
            case AugmentationSystem.ShotModifierType.Burst:
            {
                // 3연발
                burstEnabled = true;
                burstProjectileCount = Mathf.Max(1, 1 + data.projectileCountAdd);
                burstInterval = Mathf.Max(0.03f, data.burstInterval);

                // 3연발 자체 데미지 보정
                shotDamageMultiplier *= data.damageMultiplier;
                shotProjectileLifeMultiplier *= data.projectileLifeMultiplier;
                break;
            }

            case AugmentationSystem.ShotModifierType.MultiShot:
            {
                // 산탄
                multiShotEnabled = true;
                multiShotProjectileCount = Mathf.Max(1, 1 + data.projectileCountAdd);
                multiShotSpreadAngle = data.spreadAngle;

                // 산탄 자체 데미지 보정
                shotDamageMultiplier *= data.damageMultiplier;
                shotProjectileLifeMultiplier *= data.projectileLifeMultiplier;
                break;
            }

            case AugmentationSystem.ShotModifierType.Charge:
            {
                // 차지샷
                chargeShotEnabled = true;
                chargeShotTime = Mathf.Max(0f, data.chargeTime);
                chargeShotDamageMultiplier = Mathf.Max(1f, data.damageMultiplier);

                shotProjectileLifeMultiplier *= data.projectileLifeMultiplier;
                break;
            }

            case AugmentationSystem.ShotModifierType.Orbit:
            {
                // 주위탄
                shotUseOrbitProjectile = true;

                // 주위탄 개수는 Lv1 기준으로 고정.
                // Lv2 이상은 데미지/반경/속도/타격간격만 강화.
                AugmentationSystem.SubSkillLevelData baseOrbitData = aug.GetSubSkillLevelData(1);

                if (baseOrbitData != null)
                    shotOrbitProjectileCount = Mathf.Max(1, baseOrbitData.orbitProjectileCount);
                else
                    shotOrbitProjectileCount = Mathf.Max(1, data.orbitProjectileCount);

                // 효과는 현재 레벨 기준 적용
                shotOrbitRadius = data.orbitRadius;
                shotOrbitAngularSpeed = data.orbitAngularSpeed;
                shotOrbitHitInterval = data.orbitHitInterval;
                shotOrbitDamageMultiplier = data.orbitDamageMultiplier;
                shotOrbitLifetime = data.orbitLifetime;
                break;
            }
        }
    }

    /// <summary>
    /// 궤적형 서브 스킬 적용.
    /// 
    /// 조합 예:
    /// - 유도 + 반사
    /// - 유도 + 관통
    /// - 반사 + 관통
    /// 
    /// 단, 주위탄을 보유한 상태에서는
    /// AugmentRunManager에서 반사/관통 등은 후보로 뜨지 않게 막는다.
    /// </summary>
    private void ApplyTrajectorySubSkillAugment(AugmentationSystem aug, AugmentationSystem.SubSkillLevelData data,
        int level)
    {
        currentTrajectoryAugment = aug;
        currentTrajectoryLevel = level;

        // 유도 강도는 무작정 더하면 너무 과해질 수 있어서 더 큰 값만 사용.
        trajectoryHomingStrength = Mathf.Max(trajectoryHomingStrength, data.homingStrength);

        // 유도 지속시간도 더 긴 쪽 사용.
        trajectoryDuration = Mathf.Max(trajectoryDuration, data.trajectoryDuration);

        // 반사/관통은 횟수 개념이라 누적.
        trajectoryBounceCount += data.bounceCount;
        trajectoryPierceCount += data.pierceCount;
    }

    /// <summary>
    /// 효과형 서브 스킬 적용.
    /// 
    /// 조합 예:
    /// - 폭발 + 화상
    /// - 폭발 + 연쇄
    /// - 화상 + 연쇄
    /// 
    /// 단, 주위탄을 보유한 상태에서는
    /// AugmentRunManager에서 연쇄탄은 후보로 뜨지 않게 막는다.
    /// </summary>
    private void ApplyEffectSubSkillAugment(AugmentationSystem aug, AugmentationSystem.SubSkillLevelData data,
        int level)
    {
        currentEffectAugment = aug;
        currentEffectLevel = level;

        // 폭발 반경은 큰 쪽 사용.
        // 반경을 계속 더하면 화면 전체 폭발처럼 커질 수 있음.
        effectExplosionRadius = Mathf.Max(effectExplosionRadius, data.explosionRadius);

        // 폭발 데미지 배율도 큰 쪽 사용.
        effectExplosionDamageMultiplier = Mathf.Max(
            effectExplosionDamageMultiplier,
            data.explosionDamageMultiplier
        );

        // 연쇄는 횟수 개념이라 누적.
        effectChainCount += data.chainCount;

        // 연쇄 범위는 큰 쪽 사용.
        effectChainRange = Mathf.Max(effectChainRange, data.chainRange);

        // 화상 DPS는 누적 가능.
        // 예: 화상 강화 증강을 먹으면 초당 피해가 증가.
        effectDotDamagePerSecond += data.dotDamagePerSecond;

        // 화상 지속시간은 큰 쪽 사용.
        effectDotDuration = Mathf.Max(effectDotDuration, data.dotDuration);
    }

    private void ApplyPassiveAugment(AugmentationSystem aug, int stackCount)
    {
        // ------------------------------------------------------------
        // 패시브는 동일 효과 중첩형 기준으로
        // stackCount를 레벨처럼 사용
        // ------------------------------------------------------------
        int appliedLevel = Mathf.Clamp(stackCount, 1, aug.maxLevel);
        AugmentationSystem.PassiveLevelData data = aug.GetPassiveLevelData(appliedLevel);
        if (data == null) return;

        switch (aug.passiveType)
        {
            case AugmentationSystem.PassiveType.Damage:
                passiveDamageMultiplier *= data.damageMultiplier;
                break;

            case AugmentationSystem.PassiveType.AttackSpeed:
                passiveAttackSpeedPercent += data.attackSpeedPercent;
                break;

            case AugmentationSystem.PassiveType.MaxHp:
                passiveMaxHpAdd += data.maxHpAdd;
                break;

            case AugmentationSystem.PassiveType.LifeSteal:
                passiveLifeStealPercent += data.lifeStealPercent;
                break;
        }
    }

    /// <summary>
    /// 스페셜형 증강 적용.
    /// 
    /// 스페셜형은 단순 스탯 증가가 아니라
    /// 플레이 규칙을 바꾸는 능력이다.
    /// 
    /// 예:
    /// - 1회 부활
    /// - 주기적 보호막
    /// - 장애물 충돌 무시
    /// - 회복 시 자동 공격
    /// - 최대 체력 기반 데미지 증가
    /// </summary>
    private void ApplySpecialAugment(AugmentationSystem aug, int level)
    {
        if (aug == null) return;

        AugmentationSystem.SpecialLevelData data = aug.GetSpecialLevelData(level);
        if (data == null) return;

        switch (aug.specialType)
        {
            case AugmentationSystem.SpecialType.MoveSpeedBoost:
            {
                // 이동속도 특수 강화
                specialMoveSpeedMultiplier *= data.moveSpeedMultiplier;
                specialMoveSpeedAdd += data.moveSpeedAdd;
                break;
            }

            case AugmentationSystem.SpecialType.CharacterShrink:
            {
                // 캐릭터 크기 변경
                // 예: 0.8이면 80% 크기
                specialCharacterScaleMultiplier *= data.characterScaleMultiplier;
                break;
            }

            case AugmentationSystem.SpecialType.IgnoreObstacleCollision:
            {
                // 장애물 충돌 무시
                specialIgnoreObstacleCollision = data.ignoreObstacleCollision;

                // 장애물 무시와 함께 이동속도 보너스를 줄 수 있음
                specialMoveSpeedAdd += data.moveSpeedAdd;
                break;
            }

            case AugmentationSystem.SpecialType.CheatDeath:
            {
                // 치명적 피해 1회 극복
                specialCheatDeath = data.enableCheatDeath;
                specialCheatDeathHp = Mathf.Max(1, data.cheatDeathRemainHp);
                specialCheatDeathInvincibleDuration = Mathf.Max(0f, data.cheatDeathInvincibleDuration);
                break;
            }

            case AugmentationSystem.SpecialType.HpToDamageByMaxHp:
            {
                // 최대 체력 10당 최종 데미지 증가
                // 예: 0.05면 최대 체력 10당 5% 증가
                specialHpToDamagePercentPer10Hp += data.hpToDamagePercentPer10Hp;
                break;
            }

            case AugmentationSystem.SpecialType.HealOnKill:
            {
                // 적 처치 시 회복
                specialHealOnKill += data.healOnKill;
                break;
            }

            case AugmentationSystem.SpecialType.HpRegen:
            {
                // 초당 체력 회복
                specialHpRegenPerSecond += data.hpRegenPerSecond;
                break;
            }

            case AugmentationSystem.SpecialType.HealToAutoAttack:
            {
                // 회복량 누적 후 자동 공격
                // 더 좋은 수치가 들어왔을 때만 갱신
                specialHealToAutoAttackThreshold = Mathf.Max(
                    specialHealToAutoAttackThreshold,
                    data.healToAutoAttackThreshold
                );

                specialHealToAutoAttackDamageMultiplier = Mathf.Max(
                    specialHealToAutoAttackDamageMultiplier,
                    data.healToAutoAttackDamageMultiplier
                );

                break;
            }

            case AugmentationSystem.SpecialType.PeriodicShield:
            {
                // 주기적 보호막
                specialShieldInterval = Mathf.Max(specialShieldInterval, data.shieldInterval);
                specialShieldMaxCount = Mathf.Max(specialShieldMaxCount, data.shieldMaxCount);
                break;
            }
        }
    }

    private void ApplyObstacleCollisionRule(bool ignore)
    {
        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        if (obstacleLayer < 0) return;

        Physics2D.IgnoreLayerCollision(gameObject.layer, obstacleLayer, ignore);
    }
    
    /// <summary>
    /// 보호막 보유 여부에 따라 보호막 시각 효과를 켜고 끈다.
    /// 
    /// currentShieldCount가 1 이상이면 보호막 표시.
    /// 0이면 보호막 숨김.
    /// </summary>
    private void RefreshShieldVisual()
    {
        if (shieldVisualObject == null) return;

        shieldVisualObject.SetActive(currentShieldCount > 0);
    }

    #endregion

    //게임다시하기
    public void ResetPlayerForRestart()
    {
        StopAllCoroutines();
        deathUICoroutine = null;

        isDashing = false;
        dashEndTime = -999f;
        lastDashTime = -999f;
        IsDie = false;
        canControl = true;
        isFireInput = false;
        inputDirection = Vector2.zero;
        playerState = PlayerState.Idle;

        Gold = 10;

        currentWeapon = basicWeapon;
        //currentItem = null;
        equippedItems.Clear();

        RebuildPlayerStats();
        Hp = MaxHp;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        animDirection = Vector2.down;
        lockedAttackAimDirection = Vector2.down;
        aimDirection = Vector2.down;

        if (bodyAnimator != null)
        {
            bodyAnimator.speed = 1f;

            bodyAnimator.Rebind();
            bodyAnimator.Update(0f);

            bodyAnimator.ResetTrigger(AttackHash);
            bodyAnimator.ResetTrigger(HitHash);
            bodyAnimator.SetBool(IsMoveHash, false);
            bodyAnimator.SetBool(IsDeathHash, false);
            bodyAnimator.SetFloat(MoveXHash, animDirection.x);
            bodyAnimator.SetFloat(MoveYHash, animDirection.y);
        }

        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(true);
        }

        Cursor.visible = !hideSystemCursor;
        ClearOrbitProjectiles();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 메인 씬으로 돌아왔으면 플레이어는 존재하면 안 됨
        if (scene.name == "Main")
        {
            Destroy(gameObject);
            return;
        }

        // 새 씬의 Main Camera 다시 연결
        _mainCamera = Camera.main;

        // 죽은 상태가 아니면 크로스헤어 다시 켜기
        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(!IsDie);
        }

        // 증강 아이콘 UI 갱신
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }


    /// <summary>
    /// Player가 처음 생성된 직후 PlayerSpawner가 호출
    /// 
    /// 역할:
    /// 1. Crosshair 참조 연결
    /// 2. 현재 씬 Camera 연결
    /// 3. 커서/크로스헤어 상태 정리
    /// </summary>
    public void SetupAfterSpawn(Transform spawnedCrosshair)
    {
        // 새로 생성한 크로스헤어 연결
        crosshairTransform = spawnedCrosshair;

        // 현재 씬 카메라 연결
        _mainCamera = Camera.main;

        // 시스템 커서 표시 여부 설정
        Cursor.visible = !hideSystemCursor;

        // 크로스헤어가 있으면 활성화
        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 스테이지가 바뀌었을 때 새 씬 기준 참조 재연결
    /// 
    /// 역할:
    /// 1. 현재 씬 Camera 다시 잡기
    /// 2. 죽지 않았다면 크로스헤어 다시 켜기
    /// </summary>
    public void RefreshSceneReferences()
    {
        // 새 씬의 메인 카메라 다시 연결
        _mainCamera = Camera.main;

        // 살아있으면 크로스헤어 다시 켜기
        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(!IsDie);
        }
    }

    /// <summary>
    /// 스테이지 이동 직후 Player 상태를 잠깐 정리
    /// 
    /// 역할:
    /// 1. Rigidbody 속도 정지
    /// 2. 입력값 초기화
    /// 3. 공격 상태 초기화
    /// 4. 상태머신 Idle로 되돌리기
    /// </summary>
    public void ResetVelocityOnly()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        inputDirection = Vector2.zero;
        isFireInput = false;
        playerState = PlayerState.Idle;

        if (bodyAnimator != null)
        {
            bodyAnimator.SetBool(IsMoveHash, false);
            bodyAnimator.ResetTrigger(AttackHash);
        }
    }

    /// <summary>
    /// UIManager가 Main 씬으로 돌아갈 때
    /// 크로스헤어도 같이 삭제할 수 있게 반환
    /// </summary>
    public Transform GetCrosshairTransform()
    {
        return crosshairTransform;
    }


    /// <summary>
    /// 플레이어가 파괴될 때 static Instance 정리
    /// 안 해주면 죽은 오브젝트를 계속 참조할 수 있음
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 현재 씬이 실제 플레이 가능한 게임 씬인지 확인
    /// Main 씬에서는 false
    /// </summary>
    private bool IsGameplayScene()
    {
        return SceneManager.GetActiveScene().name != "Main";
    }

    public void OnUseQ()
    {
        SkillManager.Instance.UseQ();
    }

    public void OnUseE()
    {
        SkillManager.Instance.UseE();
    }
    public float GetGoldMultiplierFromItems()//* 0509
    {
        float multiplier = 1f;

        foreach (var item in equippedItems)
        {
            multiplier *= item.GetGoldMultiplier();
        }

        return multiplier;
    }
    public void MarkStatsDirty()
    {
        statsDirty = true;
    }
    public float GetShopDiscount()
    {
        float total = 0f;

        foreach (var item in equippedItems)
        {
            total += item.GetShopDiscount();
        }

        return total;
    }
    public float GetShopDiscountFromItems()
    {
        float total = 0f;

        foreach (var item in equippedItems)
        {
            if (item == null) continue;
            total += item.GetShopDiscount();
        }

        return total;
    }
    public float GetSpecialChanceAdd() //* 진열대 티켓
    {
        float add = 0f;

        foreach (var item in equippedItems)
        {
            if (item is DisplayTicket ticket)
            {
                add += ticket.specialChanceAdd;
            }
        }

        return add;
    }

    public void ApplyKnockback(Vector2 force)
    {
        if (IsDie) return;

        isDashing = false;

        isKnockback = true;
        knockbackEndTime = Time.time + knockbackDuration;

        rb.linearVelocity = Vector2.zero;

        rb.linearDamping = knockbackDrag; 
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    private void HandleKnockback()
    {
        if (!isKnockback) return;

        if (Time.time >= knockbackEndTime)
        {
            isKnockback = false;
            rb.linearDamping = 0f; 
            rb.linearVelocity *= 0.3f;
        }
    }
}