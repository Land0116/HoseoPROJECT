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
    private int baseMaxHp = 35; // 게임 시작 시 기준 최대 체력

    [SerializeField] private float baseMoveSpeed = 8.0f; // 게임 시작 시 기준 이동속도
    [SerializeField] private float baseAttackPerSecond = 1.0f; // 게임 시작 시 기준 초당 공격 횟수

    [Header("플레이어 현재 상태값")] [SerializeField]
    private int hp; // 현재 체력

    [SerializeField] private int maxHp; // 현재 최대 체력
    [SerializeField] private int gold = 10; // 현재 소지 골드
    [SerializeField] private float moveSpeed; // 현재 이동속도
    [SerializeField] private float attackPerSecond = 1.0f; //발사 주기 바뀜*
    [Header("총알 생성 보정")]
    [SerializeField] private float bulletSpawnOffset = 0.2f;

    [Header("플레이어 상태 제어")] [SerializeField]
    private PlayerState playerState = PlayerState.Idle; // 현재 상태머신 상태

    //[SerializeField] private AttackType playerAttackType = AttackType.Base; //공격 상태가 단추인지 아이템인지.
    [SerializeField] private bool isAttack = false; // 공격 쿨타임 중인지 여부
    [SerializeField] private bool isDie = false; // 사망 여부
    [SerializeField] private bool isFireInput = false; //발사입력
    [SerializeField] private bool canControl = true; // 조작 가능 여부

    [Header("비주얼 / 애니메이션")] [SerializeField]
    private Animator bodyAnimator;

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
    [SerializeField] private float itemDamage = 0f; // 아이템 공격력


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
    
    [Header("증강 - 서브 스킬 / 주위탄")]
    [SerializeField] private GameObject orbitProjectilePrefab; // 주위탄 전용 프리팹
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

    [Header("증강 - 서브 스킬 / 추가 적용값")] 
    [SerializeField] private float shotAttackSpeedBonusPercent = 0f;
    [SerializeField] private float shotProjectileLifeMultiplier = 1f;

    #endregion

    #region 증강 - 패시브형

    [Header("증강 - 패시브 / 최종 적용값")] 
    [SerializeField] private float passiveDamageMultiplier = 1f;

    [SerializeField] private float passiveAttackSpeedPercent = 0f;
    [SerializeField] private int passiveMaxHpAdd = 0;
    [SerializeField] private float passiveLifeStealPercent = 0f;
    
    [Header("증강 - 패시브 / 자동연사")]
    [SerializeField] private bool passiveAutoFireEnabled = false;
    [SerializeField] private float passiveAutoFireIntervalPercent = 0f;

    // 기본 공격 1회 발사용 입력 버퍼
    [SerializeField] private bool requestSingleShot = false;

    // 현재 누르고 있는 입력에서 이미 발사했는지
    [SerializeField] private bool firedThisPress = false;

    #endregion

    #region 증강 - 특수형

    [Header("증강 - 특수형 / 최종 적용값")] [SerializeField]
    private float specialMoveSpeedMultiplier = 1f;

    [SerializeField] private float specialMoveSpeedAdd = 0f;
    [SerializeField] private float specialCharacterScaleMultiplier = 1f;
    [SerializeField] private bool specialIgnoreObstacleCollision = false;

    [SerializeField] private bool specialCheatDeath = false;
    [SerializeField] private bool specialCheatDeathUsed = false;
    [SerializeField] private int specialCheatDeathHp = 1;
    [SerializeField] private float specialCheatDeathInvincibleDuration = 1f;

    [SerializeField] private float specialHpToDamagePercentPer10Hp = 0f;
    [SerializeField] private float specialHealOnKill = 0f;
    [SerializeField] private float specialHpRegenPerSecond = 0f;

    [SerializeField] private float specialHealToAutoAttackThreshold = 0f;
    [SerializeField] private float specialHealToAutoAttackDamageMultiplier = 0f;

    [SerializeField] private float specialShieldInterval = 0f;
    [SerializeField] private int specialShieldMaxCount = 0;
    [SerializeField] private int currentShieldCount = 0;

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

    public int Hp
    {
        get => hp;
        set => hp = Mathf.Clamp(value, 0, maxHp);
    }

    public int MaxHp => maxHp;


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

    public bool IsAttack
    {
        get => isAttack;
        set => isAttack = value;
    }

    private float DashSpeed
    {
        get
        {
            if (dashDuration <= 0f) return 0f;
            return dashDistance / dashDuration;
        }
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
        if (IsDie)
            return;

        PlayerMouseMovement();

        isPointerOverUIThisFrame = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        UpdateAnimatorLocomotion();
        CheckAttackStateRelease();

        TickSpecialRuntime();

        HandleAttack();
        UpdateAnimatorPlaybackSpeed();

    }

    void FixedUpdate()
    {
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
        if (IsDie || playerState == PlayerState.Hit || playerState == PlayerState.Death)
        {
            bodyAnimator.speed = 1f;
            return;
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

    private void HandleAttack()
    {
        if (shotUseOrbitProjectile)
        {
            // 입력 버퍼도 같이 비워서
            // 클릭을 눌러도 공격 요청이 남지 않게 처리
            requestSingleShot = false;
            firedThisPress = false;
            return;
        }

        if (!CanStartAttackNow()) return;

        // 자동연사 패시브가 없을 때
        if (!passiveAutoFireEnabled)
        {
            HandleSingleClickAttack();
            return;
        }

        // 자동연사 패시브가 있을 때
        HandleAutoFireAttack();
    }
    private void HandleSingleClickAttack()
    {
        // 이번 입력에서 아직 쏠 요청이 없으면 종료
        if (!requestSingleShot) return;

        // 이미 이번 입력에서 발사했으면 종료
        if (firedThisPress) return;

        // 차지샷이면 누르고 있는 시간 체크
        if (shotChargeTime > 0f)
        {
            // 차지 완료 전이면 아직 발사 안 함
            if (!isFireInput) return;

            if (chargeStartTime < 0f)
                chargeStartTime = Time.time;

            if (Time.time < chargeStartTime + shotChargeTime)
                return;
        }

        // 공격 쿨타임 아직 안 끝났으면 대기
        if (Time.time < nextAttackTime)
            return;

        // 실제 발사
        FireCurrentAttackPattern(false);

        // 이번 클릭 요청 소비
        requestSingleShot = false;
        firedThisPress = true;
    }
    private void HandleAutoFireAttack()
    {
        // 자동연사 패시브를 먹었더라도
        // 마우스를 안 누르고 있으면 반복 발사 안 함
        if (!isFireInput) return;

        // 차지샷이면 차지 완료 후 발사
        if (shotChargeTime > 0f)
        {
            if (chargeStartTime < 0f)
                chargeStartTime = Time.time;

            if (Time.time < chargeStartTime + shotChargeTime)
                return;
        }

        if (Time.time < nextAttackTime)
            return;

        FireCurrentAttackPattern(true);

        // 자동연사는 클릭 버퍼를 따로 유지할 필요 없음
        requestSingleShot = false;
        firedThisPress = true;
    }
    private void FireCurrentAttackPattern(bool useAutoFireIntervalCorrection)
    {
        // ------------------------------------------------------------
        // 역할:
        // 1. 현재 공격주기에 맞춰 nextAttackTime 갱신
        // 2. 차지샷이면 다음 차지를 위해 시작 시간 초기화
        // 3. 애니메이션 재생
        // 4. 현재 ShotFireMode에 맞는 발사 실행
        // ------------------------------------------------------------

        float attackInterval = GetCurrentAttackInterval(useAutoFireIntervalCorrection);
        nextAttackTime = Time.time + attackInterval;

        // 차지샷은 한 번 쏜 뒤 다시 차지 시작
        if (shotChargeTime > 0f)
        {
            chargeStartTime = Time.time;
        }

        PlayAttackAnimation();

        switch (shotFireMode)
        {
            case AugmentationSystem.ShotFireMode.Burst:
                if (burstShootCoroutine != null)
                {
                    StopCoroutine(burstShootCoroutine);
                }

                burstShootCoroutine = StartCoroutine(BurstShootRoutine());
                break;

            case AugmentationSystem.ShotFireMode.MultiShot:
                ShootMultiShot();
                break;

            default:
                ShootSingle();
                break;
        }
    }
    private float GetCurrentAttackInterval(bool useAutoFireIntervalCorrection)
    {
        // ------------------------------------------------------------
        // 기본 공격 간격 = 1 / 초당 공격 횟수
        // 예: attackPerSecond = 2 이면 0.5초마다 1발
        // ------------------------------------------------------------
        float interval = 1f / attackPerSecond;

        // ------------------------------------------------------------
        // 자동연사 패시브가 있고,
        // 현재 자동연사 발사 상황이면 간격 보정 적용
        //
        // 예:
        // autoFireIntervalPercent = 0.10 이면
        // interval * 0.9f  => 10% 더 빠르게 발사
        // ------------------------------------------------------------
        if (useAutoFireIntervalCorrection && passiveAutoFireEnabled)
        {
            float correctionMultiplier = Mathf.Clamp(1f - passiveAutoFireIntervalPercent, 0.05f, 1f);
            interval *= correctionMultiplier;
        }

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

    private IEnumerator BurstShootRoutine()
    {
        int burstCount = Mathf.Max(1, 1 + shotProjectileCountAdd);
        float interval = Mathf.Max(0.01f, shotBurstInterval);

        for (int i = 0; i < burstCount; i++)
        {
            ShootSingle();

            if (i < burstCount - 1)
                yield return new WaitForSeconds(interval);
        }

        burstShootCoroutine = null;
    }

    #endregion

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
        canControl = value;

        if (!canControl)
        {
            isDashing = false;
            dashEndTime = -999f;

            inputDirection = Vector2.zero;
            isFireInput = false;
            IsAttack = false;
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
        // ------------------------------------------------------------
        // 역할:
        // 1. 마우스가 눌렸는지/떼졌는지 상태 저장
        // 2. "이번 클릭에서 한 번 쏴야 한다"는 요청(requestSingleShot) 생성
        // 3. 패시브 자동연사가 없을 때는 이 requestSingleShot만 소비해서
        //    1회 발사만 하게 만든다.
        // ------------------------------------------------------------
        if (shotUseOrbitProjectile)
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            chargeStartTime = -1f;
            return;
        }
        if (!IsGameplayScene())
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            chargeStartTime = -1f;
            return;
        }

        if (IsDie)
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            chargeStartTime = -1f;
            return;
        }

        if (!canControl)
        {
            isFireInput = false;
            requestSingleShot = false;
            firedThisPress = false;
            chargeStartTime = -1f;
            return;
        }

        bool pressed = value.isPressed;

        // ------------------------------------------------------------
        // 마우스를 "처음" 눌렀을 때만 1회 발사 요청 생성
        // 즉, 기본 공격은 클릭 1번 = 1발 구조가 된다.
        // ------------------------------------------------------------
        if (pressed && !isFireInput)
        {
            requestSingleShot = true;
            firedThisPress = false;
            chargeStartTime = Time.time;
        }

        // ------------------------------------------------------------
        // 마우스를 떼면 현재 입력 사이클 종료
        // ------------------------------------------------------------
        if (!pressed)
        {
            firedThisPress = false;
            chargeStartTime = -1f;
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
            return;
        }

        int incomingDamage = Mathf.RoundToInt(damage);

        // 치명적 피해 극복
        if (Hp - incomingDamage <= 0 && specialCheatDeath && !specialCheatDeathUsed)
        {
            specialCheatDeathUsed = true;
            Hp = Mathf.Max(1, specialCheatDeathHp);
            invincibleUntilTime = Time.time + specialCheatDeathInvincibleDuration;
            return;
        }

        Hp -= incomingDamage;

        Debug.Log("플레이어 체력: " + Hp + " / " + maxHp);

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

        return Mathf.Max(0f, Mathf.Round(finalDamage));
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
        if (IsDie) return;

        IsDie = true;
        playerState = PlayerState.Death;
        Cursor.visible = true;

        inputDirection = Vector2.zero;
        isFireInput = false;
        IsAttack = false;

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
    
    private void RefreshOrbitProjectiles()
    {
        ClearOrbitProjectiles();

        if (!shotUseOrbitProjectile) return;
        if (shotOrbitProjectileCount <= 0) return;
        if (orbitProjectilePrefab == null) return;

        Transform orbitOwner = playerBody != null ? playerBody : transform;

        for (int i = 0; i < shotOrbitProjectileCount; i++)
        {
            float startAngle = (360f / shotOrbitProjectileCount) * i;

            GameObject orbitObj = Instantiate(
                orbitProjectilePrefab,
                orbitOwner.position,
                Quaternion.identity,
                transform // 플레이어 밑에 붙여서 씬 이동 시 같이 유지
            );

            OrbitProjectile orbit = orbitObj.GetComponent<OrbitProjectile>();
            if (orbit == null)
            {
                Destroy(orbitObj);
                continue;
            }

            float orbitDamage = GetFinalDamage() * shotOrbitDamageMultiplier;

            orbit.Initialize(
                orbitOwner,
                startAngle,
                shotOrbitRadius,
                shotOrbitAngularSpeed,
                orbitDamage,
                shotOrbitHitInterval,
                shotOrbitLifetime
            );
            orbit.SetOwner(gameObject);
            // 현재 효과형 증강도 주위탄에 같이 먹이고 싶으면 여기서 넘긴다
            orbit.SetExplosion(FinalExplosionRadius, FinalExplosionDamageMultiplier);
            orbit.SetDot(FinalDotDamagePerSecond, FinalDotDuration);

            activeOrbitProjectiles.Add(orbit);
        }
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


    //무기 및 아이템
    public void ApplyItem(ItemData item)
    {
        if (item == null) return;
        itemDamage = item.damage;
        moveSpeed = baseMoveSpeed + item.moveSpeed;
        attackPerSecond = baseAttackPerSecond + item.bulletRate;
        maxHp = baseMaxHp + item.hp;
        hp = Mathf.Clamp(Hp, 0, maxHp);

        currentItem = item;

        if (CurItemUI.Instance != null)
        {
            CurItemUI.Instance.SetItem(item);
        }
    }

    public void EquipItem(ItemData newItem)
    {
        if (newItem == null) return;


        ApplyItem(newItem);
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


    //증강 - 적 처치시 체력회복

    #region 회복 / 흡혈

    public void OnHitEnemy()
    {
        if (passiveLifeStealPercent <= 0f) return;

        float healAmount = GetFinalDamage() * passiveLifeStealPercent;
        Heal(healAmount);
    }

    public void OnKillEnemy()
    {
        if (specialHealOnKill > 0f)
        {
            Heal(specialHealOnKill, true);
        }
    }

    public void Heal(float value, bool triggerAutoAttack = true)
    {
        if (IsDie) return;
        if (value <= 0f) return;

        int beforeHp = Hp;
        Hp += Mathf.RoundToInt(value);
        int actualHealed = Hp - beforeHp;

        if (actualHealed <= 0) return;

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
            hpRegenAccumulator += specialHpRegenPerSecond * Time.deltaTime;

            if (hpRegenAccumulator >= 1f)
            {
                int healInt = Mathf.FloorToInt(hpRegenAccumulator);
                hpRegenAccumulator -= healInt;

                Heal(healInt, true);
            }
        }

        // 보호막 충전
        if (specialShieldInterval > 0f && specialShieldMaxCount > 0)
        {
            if (Time.time >= nextShieldChargeTime)
            {
                currentShieldCount = Mathf.Min(currentShieldCount + 1, specialShieldMaxCount);
                nextShieldChargeTime = Time.time + specialShieldInterval;
            }
        }
    }

    #endregion

    //UI-Esc일시정지
    private void OnPause(InputValue value)
    {
        if (!value.isPressed) return;
        if (!IsGameplayScene()) return;
        if (IsDie) return;

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

        if (currentItem != null)
        {
            ApplyItem(currentItem);
        }

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
            currentShieldCount = 0;
            nextShieldChargeTime = Time.time + specialShieldInterval;
        }
        else
        {
            currentShieldCount = 0;
            nextShieldChargeTime = -1f;
        }

        Hp = Mathf.Clamp(Hp, 0, maxHp);
        RefreshOrbitProjectiles();
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

        // 자동연사 패시브 초기값
        passiveAutoFireEnabled = false;
        passiveAutoFireIntervalPercent = 0f;

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

    private void ApplySubSkillAugment(AugmentationSystem aug, int level)
    {
        AugmentationSystem.SubSkillLevelData data = aug.GetSubSkillLevelData(level);
        if (data == null) return;

        switch (aug.subSkillType)
        {
            case AugmentationSystem.SubSkillType.Shot:
                currentShotAugment = aug;
                currentShotLevel = level;

                // -----------------------------
                // 1. 기본 수동 발사 패턴 세팅
                // -----------------------------
                shotFireMode = data.shotFireMode;
                shotProjectileCountAdd = data.projectileCountAdd;
                shotDamageMultiplier = data.damageMultiplier;
                shotSpreadAngle = data.spreadAngle;
                shotChargeTime = data.chargeTime;
                shotBurstInterval = data.burstInterval;
                shotAttackSpeedBonusPercent = data.attackSpeedBonusPercent;
                shotProjectileLifeMultiplier = data.projectileLifeMultiplier;
                
                // -----------------------------
                // 2. 주위탄 보조 시스템 세팅
                // -----------------------------
                shotUseOrbitProjectile = data.useOrbitProjectile;
                shotOrbitProjectileCount = data.orbitProjectileCount;
                shotOrbitRadius = data.orbitRadius;
                shotOrbitAngularSpeed = data.orbitAngularSpeed;
                shotOrbitHitInterval = data.orbitHitInterval;
                shotOrbitDamageMultiplier = data.orbitDamageMultiplier;
                shotOrbitLifetime = data.orbitLifetime;

                // -----------------------------
                // 3. 주위탄은 "상시 회전 보조 오브젝트"다.
                //    즉 manual shot mode와 개념이 다르다.
                //    주위탄 사용 시 manual 발사는 기본 Single 유지 권장.
                // -----------------------------
                if (shotUseOrbitProjectile)
                {
                    shotFireMode = AugmentationSystem.ShotFireMode.Single;
                    shotProjectileCountAdd = 0;
                    shotSpreadAngle = 0f;
                    shotChargeTime = 0f;
                    shotBurstInterval = 0f;
                    shotDamageMultiplier = 1f;
                }
                break;

            case AugmentationSystem.SubSkillType.Trajectory:
                currentTrajectoryAugment = aug;
                currentTrajectoryLevel = level;
                trajectoryHomingStrength = data.homingStrength;
                trajectoryDuration = data.trajectoryDuration;
                trajectoryBounceCount = data.bounceCount;
                trajectoryPierceCount = data.pierceCount;
                break;

            case AugmentationSystem.SubSkillType.Effect:
                currentEffectAugment = aug;
                currentEffectLevel = level;
                effectExplosionRadius = data.explosionRadius;
                effectExplosionDamageMultiplier = data.explosionDamageMultiplier;
                effectChainCount = data.chainCount;
                effectChainRange = data.chainRange;
                effectDotDamagePerSecond = data.dotDamagePerSecond;
                effectDotDuration = data.dotDuration;
                break;
        }
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

            case AugmentationSystem.PassiveType.AutoFire:
                passiveAutoFireEnabled = data.autoFireEnabled;
                passiveAutoFireIntervalPercent = Mathf.Max(
                    passiveAutoFireIntervalPercent,
                    data.autoFireIntervalPercent
                );
                break;
        }
    }

    private void ApplySpecialAugment(AugmentationSystem aug, int level)
    {
        AugmentationSystem.SpecialLevelData data = aug.GetSpecialLevelData(level);
        if (data == null) return;

        switch (aug.specialType)
        {
            case AugmentationSystem.SpecialType.MoveSpeedBoost:
                specialMoveSpeedMultiplier *= data.moveSpeedMultiplier;
                specialMoveSpeedAdd += data.moveSpeedAdd;
                break;

            case AugmentationSystem.SpecialType.CharacterShrink:
                specialCharacterScaleMultiplier *= data.characterScaleMultiplier;
                break;

            case AugmentationSystem.SpecialType.IgnoreObstacleCollision:
                specialIgnoreObstacleCollision = data.ignoreObstacleCollision;
                specialMoveSpeedAdd += data.moveSpeedAdd;
                break;

            case AugmentationSystem.SpecialType.CheatDeath:
                specialCheatDeath = data.enableCheatDeath;
                specialCheatDeathHp = data.cheatDeathRemainHp;
                specialCheatDeathInvincibleDuration = data.cheatDeathInvincibleDuration;
                break;

            case AugmentationSystem.SpecialType.HpToDamageByMaxHp:
                specialHpToDamagePercentPer10Hp += data.hpToDamagePercentPer10Hp;
                break;

            case AugmentationSystem.SpecialType.HealOnKill:
                specialHealOnKill += data.healOnKill;
                break;

            case AugmentationSystem.SpecialType.HpRegen:
                specialHpRegenPerSecond += data.hpRegenPerSecond;
                break;

            case AugmentationSystem.SpecialType.HealToAutoAttack:
                specialHealToAutoAttackThreshold = data.healToAutoAttackThreshold;
                specialHealToAutoAttackDamageMultiplier = data.healToAutoAttackDamageMultiplier;
                break;

            case AugmentationSystem.SpecialType.PeriodicShield:
                specialShieldInterval = data.shieldInterval;
                specialShieldMaxCount = Mathf.Max(specialShieldMaxCount, data.shieldMaxCount);
                break;
        }
    }
    
    private void ApplyObstacleCollisionRule(bool ignore)
    {
        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        if (obstacleLayer < 0) return;

        Physics2D.IgnoreLayerCollision(gameObject.layer, obstacleLayer, ignore);
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
        IsAttack = false;
        inputDirection = Vector2.zero;
        playerState = PlayerState.Idle;

        Gold = 10;

        currentWeapon = basicWeapon;
        currentItem = null;

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
        IsAttack = false;
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

    public void OnShop(InputValue value)
    {

        if (!value.isPressed) return;
        Debug.Log("B");

        if (InGameShopUIManager.Instance == null)
        {
            Debug.Log(" ShopUIManager 없음");
            return;
        }

        Debug.Log(" ShopUIManager 있음");

        InGameShopUIManager.Instance.ToggleShop();
    }
    public void OnInventory(InputValue value)
    {
        if (!value.isPressed) return;

        if (!IsGameplayScene()) return;
        if (IsDie) return;

        if (InventoryManager.Instance == null)
        {
            Debug.Log("InventoryManager 없음");
            return;
        }

        if (!InventoryManager.Instance.canOpenInventory)
        {
            Debug.Log("인벤토리 비활성 상태");
            return;
        }

        // 상점 열려있으면 먼저 닫기
        if (InGameShopUIManager.Instance != null && InGameShopUIManager.Instance.IsShopOpen())
        {
            InGameShopUIManager.Instance.CloseShop();
            return;
        }

        InventoryManager.Instance.ToggleInventory();
    }

    private void OnUseQ(InputValue value)
    {
        if (!value.isPressed) return;

        PlayerUIManager.Instance.UseEquipment(EquipmentSlot.SlotType.Q);
    }

    private void OnUseE(InputValue value)
    {
        if (!value.isPressed) return;

        PlayerUIManager.Instance.UseEquipment(EquipmentSlot.SlotType.E);
    }
}
