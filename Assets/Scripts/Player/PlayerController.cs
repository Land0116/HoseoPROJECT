using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("플레이어 기본 정보")] 
    [SerializeField] private int baseMaxHp = 50; // 게임 시작 시 기준 최대 체력
    [SerializeField] private float baseMoveSpeed = 3.0f; // 게임 시작 시 기준 이동속도
    [SerializeField] private float baseAttackPerSecond = 1.0f; // 게임 시작 시 기준 초당 공격 횟수

    [Header("플레이어 현재 상태값")] 
    [SerializeField] private int hp; // 현재 체력
    [SerializeField] private int maxHp; // 현재 최대 체력
    [SerializeField] private int gold; // 현재 소지 골드
    [SerializeField] private float moveSpeed = 3.0f; // 현재 이동속도
    [SerializeField] private float attackPerSecond = 1.0f; //발사 주기 바뀜*

    [Header("플레이어 상태 제어")] 
    [SerializeField] private PlayerState playerState = PlayerState.Idle; // 현재 상태머신 상태
    [SerializeField] private AttackType playerAttackType = AttackType.Base; //공격 상태가 단추인지 아이템인지.
    [SerializeField] private bool isAttack = false; // 공격 쿨타임 중인지 여부
    [SerializeField] private bool isDie = false; // 사망 여부
    [SerializeField] private bool isFireInput = false; //발사입력
    [SerializeField] private bool canControl = true; // 조작 가능 여부

    [Header("장비")]
    //[SerializeField] private Weapon currentWeapon; //현재 장착 중인 총알
    //[SerializeField] private Weapon basicWeapon; // 시작 무기
    [SerializeField] private WeaponData basicWeapon;
    [SerializeField] private WeaponData currentWeapon;
    
    [SerializeField] private Item currentItem; // 현재 장착 중인 아이템
    [SerializeField] private GameObject curProjectilePrefab; // 현재 발사할 투사체 프리팹
    [SerializeField] private float weaponDamage; // 무기 기본 공격력 (기존 attackDamage를 무기 공격력으로 사용)
    [SerializeField] private float itemDamage = 0f; // 아이템 공격력


//     [Header("데미지 계산 변수 + 증강 시스템 계산 변수")]
// // 무기 증강 시너지
//     //[SerializeField] private float weaponSynergy = 1f;
// // 아이템 증강 시너지
//     //[SerializeField] private float itemSynergy = 1f;

    [Header("증강 - 공격")] 
    [SerializeField] private float damageMultiplier = 1f; // 총알/아이템 공통 데미지 배율
    [SerializeField] private float slowDamageMultiplier = 0f; // 느리지만 강한 공격 추가 배율
    [SerializeField] private float hpToDamagePercentPer10Hp = 0f; // 최대 체력 10당 최종 데미지 증가율
    [SerializeField] private int bulletPerShot = 1; // 총알 개수증가

    [SerializeField] private float bulletSpreadAngle = 10f; // 2개의 총알 날아가는 방향각

//유틸 및 수비형 증강
    [Header("증강 - 체력 / 방어")] 
    [SerializeField] private int healOnKillAmount = 0;
    [SerializeField] private float hpRegenPerSecond = 0f;
    [SerializeField] private float lifeStealAmount = 0f;

    [SerializeField] private bool shieldEnabled = false;
    [SerializeField] private bool shieldReady = false;
    [SerializeField] private float shieldInterval = 30f;


    [Header("증강 - 회복 자동 공격")] 
    [SerializeField] private bool isHealToAutoAttackActive = false; // 증강 활성화 여부
    [SerializeField] private int accumulatedHealAmount = 0; // 실제 누적 회복량
    [SerializeField] private int autoAttackHealThreshold = 5; // 회복량 5당 1회 발동
    [SerializeField] private float autoAttackDamageRatio = 0.5f; // 최종 데미지 x 0.5

    [Header("증강 - 이동 / 유틸 / 치명피해 1회무시")] 
    [SerializeField] private bool ignoreObstacle = false; // 현재 장애물 충돌 무시 증강이 적용되어 있는지
    [SerializeField] private string obstacleLayerName = "Obstacle"; // 장애물 레이어 이름
    [SerializeField] private string enemyLayerName = "Monster"; // 몬스터 레이어 ( 몬스터도 장애물 무시하기에 충돌 유지를 위함. )
    [SerializeField] private bool cheatDeathOnce = false; // 1회 부활 효과 여부
    [SerializeField] private float characterScaleMultiplier = 1f; // 캐릭터 크기 누적 배율
    private Vector3 originalScale; // 게임 시작 시 원본 스케일
    [SerializeField] private float invincibleDuration = 1f; // 치명적 피해 극복 후 무적 시간
    [SerializeField] private bool isInvincible = false; // 현재 무적 여부


    [Header("컴포넌트 참조")] 
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 inputDirection;
    private Camera _mainCamera;

    [Header("조준점 설정")] 
    [SerializeField] private Transform crosshairTransform; // 계층 구조의 Crosshair 오브젝트 연결
    [SerializeField] private bool hideSystemCursor = true; // 시스템 커서 숨김 여부
    [SerializeField] private Transform playerBody;

    [Header("총알 소환 위치 관련")] 
    public Transform gunTip;

    private Coroutine shieldCoroutine;
    private Coroutine hpRegenCoroutine;
    [SerializeField] private string playerSpawnTag = "PlayerSpawnPoint";

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

    public bool IsDie
    {
        get => isDie;
        set => isDie = value;
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        originalScale = transform.localScale;
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _mainCamera = Camera.main;

        maxHp = baseMaxHp; //체력수정.아이템
        Hp = maxHp;

        //weaponDamage = AttackDamage;
        if (currentWeapon == null)
        {
            currentWeapon = basicWeapon;
        }

        if (currentWeapon != null)
        {
            ApplyWeapon(currentWeapon);
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
        RebuildPlayerStats();

        if (AugUIManager.instance != null && AugmentRunManager.Instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsDie)
        {
            return;
        }

        PlayerMouseMovement();
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

        switch (playerState)
        {
            case PlayerState.Idle:
            {
                rb.linearVelocity = Vector2.zero;

                if (inputDirection.sqrMagnitude > 0)
                {
                    playerState = PlayerState.Walk;
                    break;
                }

                // 공격 입력
                if (isFireInput && !IsAttack)
                {
                    playerState = PlayerState.Attack;
                }

                break;
            }
            case PlayerState.Walk:
            {
                Vector2 moveVector = inputDirection;
                if (moveVector.magnitude > 1)
                {
                    moveVector.Normalize();
                }

                rb.linearVelocity = moveVector * moveSpeed;
                if (inputDirection.sqrMagnitude == 0)
                {
                    playerState = PlayerState.Idle;
                }

                // 이동 중 공격
                if (isFireInput && !IsAttack) // &&amount > 0 && !isReloading)
                {
                    playerState = PlayerState.Attack;
                }

                break;
            }
            case PlayerState.Attack:
            {
                Shoot();
                playerState = inputDirection.sqrMagnitude > 0
                    ? PlayerState.Walk
                    : PlayerState.Idle;

                break;
            }
            case PlayerState.Hit:
            {
                playerState = PlayerState.Idle;
                //피격 애니메이션
                break;
            }
            case PlayerState.Death:
            {
                // 죽은 상태에서는 이동도 공격도 하지 않음
                rb.linearVelocity = Vector2.zero;
                break;
            }
        }
    }


    void PlayerMouseMovement()
    {
        if (IsDie) return;
        if (crosshairTransform == null || playerBody == null) return;

        // 1. 마우스 월드 좌표 계산 (한 번만 수행)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            -_mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;

        // 2. 조준점 위치 업데이트
        crosshairTransform.position = mouseWorldPos;

        // 3. 플레이어 회전 계산 (조준점 위치를 바로 활용)
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)playerBody.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        playerBody.rotation = Quaternion.Euler(0, 0, angle);
    }

    //단추(기본공격) 발사
    void Shoot()
    {
        if (IsDie) return;
        if (!IsAttack && playerAttackType == AttackType.Base)
        {
            if (curProjectilePrefab == null) return;

            int currentBulletCount = Mathf.Max(1, bulletPerShot);

            float startAngle = -bulletSpreadAngle * (currentBulletCount - 1) * 0.5f;

            for (int i = 0; i < currentBulletCount; i++)
            {
                float addAngle = startAngle + (bulletSpreadAngle * i);
                Quaternion bulletRotation = playerBody.rotation * Quaternion.Euler(0f, 0f, addAngle);

                GameObject bullet = Instantiate(curProjectilePrefab, gunTip.position, bulletRotation);
                ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

                if (bulletScript != null)
                {
                    bulletScript.SetDamage(GetFinalDamage());
                }
            }

            StartCoroutine(FireRate());
        }
    }

    public void SetControl(bool value)
    {
        canControl = value;

        if (!canControl)
        {
            inputDirection = Vector2.zero;
            isFireInput = false;
            IsAttack = false;
            rb.linearVelocity = Vector2.zero;
            playerState = PlayerState.Idle;
        }
    }

    //유니티 기본 InputSystem
    private void OnMove(InputValue movementValue)
    {
        if (IsDie)
        {
            inputDirection = Vector2.zero;
            return;
        }

        if (!canControl)
        {
            inputDirection = Vector2.zero;
            return;
        }

        inputDirection = movementValue.Get<Vector2>();
        if (inputDirection.sqrMagnitude > 0)
        {
            playerState = PlayerState.Walk;
        }
        else
        {
            playerState = PlayerState.Idle;
        }
    }

    private void OnAttack(InputValue value)
    {
        if (IsDie)
        {
            isFireInput = false;
            return;
        }

        if (!canControl)
        {
            isFireInput = false;
            return;
        }

        isFireInput = value.isPressed;
    }

    //플레이어 데미지 계산
    public void OnDamage(float damage)
    {
        // 이미 죽은 상태면 피격 무시
        if (IsDie) return;

        // 일시 무적 상태면 피격 무시
        if (isInvincible) return;

        // 보호막이 준비되어 있으면 공격 1회 무효화
        if (shieldEnabled && shieldReady)
        {
            shieldReady = false;
            Debug.Log("보호막으로 공격 1회 무효");
            return;
        }

        // 들어온 데미지를 정수형으로 변환
        int incomingDamage = Mathf.RoundToInt(damage);

        // 치명적인 피해를 받는 순간 1회만 사망 무효
        if (cheatDeathOnce && Hp - incomingDamage <= 0)
        {
            cheatDeathOnce = false; // 한 번 발동하면 소모
            Hp = 1; // 최소 체력 1 남김
            playerState = PlayerState.Hit;

            // 짧은 무적 시간 부여
            StartCoroutine(TemporaryInvincibleRoutine());

            Debug.Log("치명적 피해 1회 무효 발동");
            return;
        }

        // 일반 피격 처리
        Hp -= incomingDamage;
        Debug.Log("플레이어 체력: " + Hp + " / " + maxHp);
        playerState = PlayerState.Hit;

        // 체력이 0 이하가 되면 사망 처리
        if (Hp <= 0)
        {
            Death();
        }
    }

    //최종데미지 계산
    public float GetFinalDamage()
    {
        float baseDamage = weaponDamage + itemDamage;

        // 기본 데미지 배율 적용
        float damageByMultiplier = baseDamage * damageMultiplier;

        // 느리지만 강한 공격 추가 보너스
        float slowBonusDamage = baseDamage * slowDamageMultiplier;


        float subtotal = damageByMultiplier + slowBonusDamage;

        int hpStack = maxHp / 10;
        float hpBonusDamage = subtotal * (hpStack * hpToDamagePercentPer10Hp);

        float finalDamage = subtotal + hpBonusDamage;
        Debug.Log(
            $"[DamageCheck] base:{baseDamage}, maxHp:{maxHp}, hpStack:{hpStack}, " +
            $"hpRate:{hpToDamagePercentPer10Hp}, hpBonus:{hpBonusDamage}, final:{finalDamage}"
        );
        // 음수 방지 + 보기 좋은 값으로 반올림
        return Mathf.Max(0f, Mathf.Round(finalDamage));
    }

    public void Death()
    {
        if (IsDie) return;
        IsDie = true;
        playerState = PlayerState.Death;
        Cursor.visible = true;

        // 플레이어 입력 관련 값 전부 초기화
        // → 키를 누르고 있던 상태가 남아있지 않게 처리
        inputDirection = Vector2.zero;
        isFireInput = false;
        IsAttack = false;


        // 이동 완전 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 혹시 외부에서 canControl을 같이 사용 중이면
        // 죽은 뒤 조작 금지 상태로 같이 잠가줌
        canControl = false;

        // 이 PlayerController에서 돌고 있던 코루틴 정지
        // → 공격 쿨타임, 장전, 보호막, 체젠 등 남아있는 동작 정리
        StopAllCoroutines();

        // 조준점이 있으면 꺼줌
        // → 죽은 뒤 크로스헤어가 계속 움직여 보이는 문제 방지
        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(false);
        }

        PlayerUIManager.instance.ShowPlayerDeathUI();
    }

    //힐
    public void Heal(float value)
    {
        // 죽은 상태면 회복 처리 안 함
        if (IsDie) return;

        // 잘못된 회복값 방지
        if (value <= 0f) return;

        // 회복 전 체력 저장
        int beforeHp = Hp;

        // 실제 회복 적용
        Hp += Mathf.RoundToInt(value);

        // 실제로 회복된 양 계산
        // 최대 체력에 막히면 요청한 회복량보다 적게 찰 수 있음
        int actualHealed = Hp - beforeHp;

        // 실제 회복량이 0 이하라면 종료
        if (actualHealed <= 0) return;

        // 회복 -> 자동 공격 증강이 활성화된 경우에만 누적
        if (isHealToAutoAttackActive)
        {
            accumulatedHealAmount += actualHealed;

            // 누적 회복량이 기준치(기본 5)를 넘을 때마다 자동 공격 1회 발동
            while (accumulatedHealAmount >= autoAttackHealThreshold)
            {
                accumulatedHealAmount -= autoAttackHealThreshold;
                SpawnHealAutoAttack();
            }
        }
    }

    //체력흡수
    public void OnHitEnemy()
    {
        if (lifeStealAmount <= 0f) return;

        Heal(lifeStealAmount);
    }

    //증강시스템
    public void ApplyAugmentation(AugmentationSystem aug)
    {
        // 잘못된 참조 방지
        if (aug == null) return;

        switch (aug.effectType)
        {
            // =========================
            // [최종 데미지 관련]
            // =========================
            case AugmentationSystem.AugmentEffectType.DamageMultiplier:
                damageMultiplier *= aug.value;
                AttackPerSecond += aug.duration;
                break;

            case AugmentationSystem.AugmentEffectType.SlowDamageMultiplier:
                slowDamageMultiplier += aug.value;
                break;

            // =========================
            // [공격 관련]
            // =========================
            case AugmentationSystem.AugmentEffectType.BulletCountAdd:
                bulletPerShot += Mathf.RoundToInt(aug.value);
                bulletPerShot = Mathf.Max(1, bulletPerShot);
                break;

            case AugmentationSystem.AugmentEffectType.AttackPerSecondAdd:
                // 공격속도증가
                AttackPerSecond += aug.value;
                break;

            // =========================
            // [체력 / 방어 관련]
            // =========================
            case AugmentationSystem.AugmentEffectType.MaxHpMultiplier:
            {
                // 최대 체력 배율 적용
                // 현재 체력 비율을 유지한 채 최대 체력 갱신
                int oldMaxHp = maxHp;
                maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * aug.value));

                if (oldMaxHp > 0)
                {
                    float hpRatio = (float)Hp / oldMaxHp;
                    Hp = Mathf.RoundToInt(maxHp * hpRatio);
                }
                else
                {
                    Hp = maxHp;
                }

                break;
            }

            case AugmentationSystem.AugmentEffectType.HpToDamagePercentPerHp:
                // enum 이름은 PerHp지만,
                // 실제 사용 의도는 "최대 체력 10당 최종 데미지 증가율"로 처리
                hpToDamagePercentPer10Hp += aug.value;
                break;

            case AugmentationSystem.AugmentEffectType.HealOnKill:
                // 적 처치 시 회복량 증가
                healOnKillAmount += Mathf.RoundToInt(aug.value);
                break;

            case AugmentationSystem.AugmentEffectType.HpRegenPerSecond:
                // 초당 체력 회복량 누적
                hpRegenPerSecond += aug.value;

                // 체젠 코루틴이 아직 없으면 시작
                if (hpRegenCoroutine == null)
                {
                    hpRegenCoroutine = StartCoroutine(HpRegenRoutine());
                }

                break;

            case AugmentationSystem.AugmentEffectType.HealToAutoAttack:
                // 이 증강은 별도 전용 로직 사용
                // value, duration에 의존하지 않고 bool만 켬
                isHealToAutoAttackActive = true;
                break;

            case AugmentationSystem.AugmentEffectType.LifeStealOnHit:
                // 공격 적중 시 회복량 누적
                lifeStealAmount += aug.value;
                break;

            case AugmentationSystem.AugmentEffectType.ShieldByInterval:
                // 보호막 기능 활성화
                shieldEnabled = true;

                // duration이 있으면 duration 우선 사용
                // 없으면 value를 보호막 주기로 사용
                if (aug.duration > 0f)
                {
                    shieldInterval = aug.duration;
                }
                else if (aug.value > 0f)
                {
                    shieldInterval = aug.value;
                }

                // 기존 보호막 코루틴이 있으면 재시작
                if (shieldCoroutine != null)
                {
                    StopCoroutine(shieldCoroutine);
                }

                shieldCoroutine = StartCoroutine(ShieldRoutine());
                break;

            // =========================
            // [이동 / 유틸 관련]
            // =========================
            case AugmentationSystem.AugmentEffectType.MoveSpeedMultiplier:
                // 이동속도 배율 적용
                MoveSpeed *= aug.value;
                break;

            case AugmentationSystem.AugmentEffectType.CharacterScaleMultiplier:
                // 누적 배율을 별도 저장한 뒤
                // 원본 스케일 기준으로 다시 계산
                characterScaleMultiplier *= aug.value;
                transform.localScale = originalScale * characterScaleMultiplier;
                break;

            case AugmentationSystem.AugmentEffectType.IgnoreObstacleCollision:
                // 장애물 충돌 무시 활성화
                ignoreObstacle = true;
                ApplyObstacleCollisionState();
                break;

            case AugmentationSystem.AugmentEffectType.CheatDeathOnce:
                // 1회만 치명상 무효
                cheatDeathOnce = true;
                break;
        }
    }

    //증강 - 적 처치시 체력회복
    public void OnKillEnemy()
    {
        if (healOnKillAmount <= 0) return;

        // 설정된 회복량만큼 회복
        Heal(healOnKillAmount);
    }

    //[ 아이템 및 단추(총알) ]
    public void ApplyItem(Item item)
    {
        if (item == null) return;
        itemDamage = item.damage;
        moveSpeed = baseMoveSpeed + item.moveSpeed;
        AttackPerSecond = baseAttackPerSecond + item.bulletRate;
        maxHp = baseMaxHp + item.hp;
        Hp = Mathf.Clamp(Hp, 0, maxHp);

        currentItem = item;
    }

    public void EquipItem(Item newItem)
    {
        if (newItem == null) return;

        ApplyItem(newItem);
    }

    public void ApplyWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        weaponDamage = weapon.damage;
        curProjectilePrefab = weapon.projectilePrefab;
        
    }
    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return;

        currentWeapon = newWeapon;   // 현재 무기 저장
        ApplyWeapon(currentWeapon);  // 실제 적용
    }

    //증강 - 자동 공격
    private void SpawnHealAutoAttack()
    {
        // 필수 참조가 없으면 종료
        if (curProjectilePrefab == null) return;
        if (gunTip == null) return;
        if (playerBody == null) return;

        // 플레이어가 현재 보는 방향으로 자동 공격 총알 1개 생성
        GameObject bullet = Instantiate(curProjectilePrefab, gunTip.position, playerBody.rotation);
        ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

        // 총알 스크립트가 있으면 데미지 적용
        if (bulletScript != null)
        {
            float autoAttackDamage = GetFinalDamage() * autoAttackDamageRatio;
            bulletScript.SetDamage(autoAttackDamage);
        }
    }

    //장애물 충돌 무시 
    private void ApplyObstacleCollisionState()
    {
        int playerLayer = gameObject.layer;
        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (obstacleLayer == -1)
        {
            Debug.LogWarning($"장애물 레이어 이름이 잘못됨 : {obstacleLayerName}");
            return;
        }

        // 장애물만 무시
        Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, ignoreObstacle);

        // 몬스터는 항상 충돌 유지
        if (enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
    }

    //발사호출
    IEnumerator FireRate()
    {
        IsAttack = true;
        yield return new WaitForSeconds(1f / AttackPerSecond);
        IsAttack = false;
    }

    //쉴드 
    private IEnumerator ShieldRoutine()
    {
        shieldReady = true;

        while (shieldEnabled)
        {
            if (!shieldReady)
            {
                yield return new WaitForSeconds(shieldInterval);
                shieldReady = true;
                Debug.Log("보호막 재충전 완료");
            }

            yield return null;
        }
    }

    //힐 리젠
    private IEnumerator HpRegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (Hp > 0 && Hp < MaxHp)
            {
                Heal(hpRegenPerSecond);
            }
        }
    }

    //치명상 무효 후 잠깐 무적
    private IEnumerator TemporaryInvincibleRoutine()
    {
        // 무적 시작
        isInvincible = true;

        // 설정한 무적 시간 동안 대기
        yield return new WaitForSeconds(invincibleDuration);

        // 무적 종료
        isInvincible = false;
    }


    //증강변수 초기화
    public void RebuildPlayerStats()
    {
        // =========================
        // 1. 플레이어 기본 스탯 복원
        // =========================
        maxHp = baseMaxHp;
        moveSpeed = baseMoveSpeed;
        AttackPerSecond = baseAttackPerSecond;

        // 무기/아이템 기본 데미지 초기화
        weaponDamage = 0f;
        itemDamage = 0f;
        curProjectilePrefab = null;

        // =========================
        // 2. 증강 런타임 값 초기화
        // =========================
        damageMultiplier = 1f;
        slowDamageMultiplier = 0f;
        bulletPerShot = 1;
        hpToDamagePercentPer10Hp = 0f;

        healOnKillAmount = 0;
        hpRegenPerSecond = 0f;
        lifeStealAmount = 0f;

        shieldEnabled = false;
        shieldReady = false;
        shieldInterval = 30f;

        isHealToAutoAttackActive = false;
        accumulatedHealAmount = 0;
        autoAttackHealThreshold = 5;
        autoAttackDamageRatio = 0.5f;

        ignoreObstacle = false;
        cheatDeathOnce = false;

        characterScaleMultiplier = 1f;
        transform.localScale = originalScale;

        // =========================
        // 3. 증강 관련 코루틴 정리
        // =========================
        if (hpRegenCoroutine != null)
        {
            StopCoroutine(hpRegenCoroutine);
            hpRegenCoroutine = null;
        }

        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        // =========================
        // 4. 현재 장착 무기 다시 적용
        // =========================
        if (basicWeapon != null)
        {
            ApplyWeapon(basicWeapon);
        }
        
        if (currentWeapon == null)
        {
            currentWeapon = basicWeapon;
        }
        if (currentWeapon != null)
        {
            ApplyWeapon(currentWeapon);
        }
        // =========================
        // 5. 현재 장착 아이템 다시 적용
        // =========================
        if (currentItem != null)
        {
            ApplyItem(currentItem);
        }

        // =========================
        // 6. 현재 보유 증강 전부 다시 적용
        // =========================
        if (AugmentRunManager.Instance != null)
        {
            for (int i = 0; i < AugmentRunManager.Instance.OwnedAugments.Count; i++)
            {
                ApplyAugmentation(AugmentRunManager.Instance.OwnedAugments[i]);
            }
        }

        // =========================
        // 7. 체력 범위 보정 및 충돌 상태 반영
        // =========================
        Hp = Mathf.Clamp(Hp, 0, maxHp);
        ApplyObstacleCollisionState();
    }
    
    //게임다시하기
    public void ResetPlayerForRestart()
    {
        StopAllCoroutines();

        IsDie = false;
        canControl = true;
        isInvincible = false;
        isFireInput = false;
        IsAttack = false;
        inputDirection = Vector2.zero;
        playerState = PlayerState.Idle;

        Gold = 0;

        // 완전 처음부터 시작이면 기본 무기로 되돌림
        currentWeapon = basicWeapon;

        // 시작 아이템이 없는 구조면 null
        currentItem = null;

        RebuildPlayerStats();
        Hp = MaxHp;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(true);
        }

        Cursor.visible = !hideSystemCursor;
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
        // 새 씬의 Main Camera 다시 연결
        _mainCamera = Camera.main;

        // 죽은 상태가 아니면 크로스헤어 다시 켜기
        if (crosshairTransform != null)
        {
            crosshairTransform.gameObject.SetActive(!IsDie);
        }

        // 새 씬에 스폰 포인트가 있으면 그 위치로 이동
        GameObject spawnPoint = GameObject.FindGameObjectWithTag(playerSpawnTag);
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        // UI가 플레이어 참조를 다시 잡게 함
        if (PlayerUIManager.instance != null)
        {
            PlayerUIManager.instance.BindPlayer(this);
        }

        // 증강 아이콘 UI 갱신
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }
}