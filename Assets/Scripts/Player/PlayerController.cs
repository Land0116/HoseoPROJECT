using System;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;
using System.Runtime.CompilerServices;
using UnityEditor.ShaderGraph.Internal;


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

    [Header("플레이어 정보")]
    [SerializeField] private int baseMaxHP = 50;
    [SerializeField] private int hp; //playerHP
    [SerializeField] private int maxHp = 50; //playerMaxHP
    [SerializeField] private int gold; //playerGold
    [SerializeField] private float moveSpeed = 3.0f; //playerMoveSpeed;
    [SerializeField] private float baseMoveSpeed = 3.0f; //playerMoveSpeed;
    [SerializeField] private float attackDamage = 10; //playerDamage;
    
    
    
    
    [Header("플레이어의 상태")] 
    [SerializeField] private PlayerState playerState = PlayerState.Idle;
    [SerializeField] private AttackType playerAttackType = AttackType.Base;
    [SerializeField] private float attackPerSecond = 1.0f; //발사 주기 바뀜*
    [SerializeField] private float baseAttackPerSecond = 1.0f;
    [SerializeField] private bool isAttack = false; //발사 쿨타임?
    [SerializeField] private bool isDie = false;
    
    
    [SerializeField] private bool isFireInput = false; //발사입력

    [Header("장착 아이템")]
    [SerializeField] private Item currentItem;

    [Header("데미지 계산 변수")]

// 무기 기본 공격력 (기존 attackDamage를 무기 공격력으로 사용)
    [SerializeField] private float weaponDamage;
// 아이템 공격력
    [SerializeField] private float itemDamage = 0f;
// 무기 증강 시너지
    [SerializeField] private float weaponSynergy = 1f;
// 아이템 증강 시너지
    [SerializeField] private float itemSynergy = 1f;
    [SerializeField] private int bulletPerShot = 1;
    [SerializeField] private float bulletSpreadAngle = 10f;

    [SerializeField] private bool canControl = true;

    [SerializeField] private bool shieldEnabled = false;
    [SerializeField] private bool shieldReady = false;
    [SerializeField] private float shieldInterval = 30f;

    [SerializeField] private int hpRegenAmount = 0;
    [SerializeField] private float hpRegenInterval = 10f;

    [SerializeField] private float lifeStealAmount = 0f;
    [SerializeField] private bool ignoreObstacle = false;
    [SerializeField] private bool removeSightBlock = false;
    [SerializeField] private string obstacleLayerName = "Obstacle";

    private Coroutine shieldCoroutine;
    private Coroutine hpRegenCoroutine;

    public static Action OnRemoveSightBlock;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 inputDirection;
    
    [Header("조준점 설정")]
    [SerializeField] private Transform crosshairTransform; // 계층 구조의 Crosshair 오브젝트 연결
    [SerializeField] private bool hideSystemCursor = true; // 시스템 커서 숨김 여부
    [SerializeField] private Transform playerBody;
    
    private Camera _mainCamera;
    
    
    [Header("공격 관련")]
    public GameObject bulletPrefab;
    public Transform gunTip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public int Hp
    {
        get => hp;
        set => hp = Mathf.Clamp(value,0,maxHp);
    }
    public int MaxHp => maxHp;

    

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0,value);
    }

    public int Gold
    {
        get => gold;
        set => gold = Mathf.Max(0, value);
    }
    
    public float AttackDamage
    {
        get => attackDamage;
        set => attackDamage = Mathf.Max(0, value);
    }

    public float FireRateTime
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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _mainCamera = Camera.main;



        maxHp = baseMaxHP;//체력수정.아이템
        Hp = maxHp;
        
        weaponDamage = AttackDamage;

        moveSpeed = baseMoveSpeed; //아이템관련 수정
        if (currentItem != null)//수정한 부분.아이템
        {
            ApplyItem(currentItem);
        }
    }
    

    void Start()
    {
        if (hideSystemCursor) Cursor.visible = false;
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
                if (moveVector.magnitude > 1) { moveVector.Normalize(); }
                rb.linearVelocity = moveVector * moveSpeed;
                if (inputDirection.sqrMagnitude == 0)
                {
                    playerState = PlayerState.Idle;
                }

                // 이동 중 공격
                if (isFireInput && !IsAttack )// &&amount > 0 && !isReloading)
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
        if (!IsAttack && playerAttackType == AttackType.Base)// && amount > 0)
        {
            int currentBulletCount = Mathf.Max(1, bulletPerShot);

            float startAngle = -bulletSpreadAngle * (currentBulletCount - 1) * 0.5f;

            for (int i = 0; i < currentBulletCount; i++)
            {
                float addAngle = startAngle + (bulletSpreadAngle * i);
                Quaternion bulletRotation = playerBody.rotation * Quaternion.Euler(0f, 0f, addAngle);

                GameObject bullet = Instantiate(bulletPrefab, gunTip.position, bulletRotation);
                ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

                if (bulletScript != null)
                {
                    bulletScript.SetDamage(GetFinalDamage());
                }
            }

            //amount--;
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
        if (inputDirection.sqrMagnitude > 0 ) { playerState = PlayerState.Walk; }
        else { playerState = PlayerState.Idle; }
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

    public void OnDamage(float damage)
    {
        if (IsDie) return;
        if (shieldEnabled && shieldReady)
        {
            shieldReady = false;
            Debug.Log("보호막으로 공격 1회 무효");
            return;
        }

        hp -= Mathf.RoundToInt(damage);
        Debug.Log("플레이어 체력: " + hp + " / " + maxHp);
        playerState = PlayerState.Hit;

        if (hp <= 0)
        {
            Death();
        }
    }
    
    public float GetFinalDamage()
    {
        float finalDamage =
            (weaponDamage * weaponSynergy) +
            (itemDamage * itemSynergy);

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

    }
    
    public void Heal(float value)
    {
        //hp += Mathf.RoundToInt(value);
        //hp = Mathf.Clamp(hp, 0, maxHp);
        
        Hp += Mathf.RoundToInt(value);
    }

    public void OnHitEnemy(float damage)
    {
        if (lifeStealAmount <= 0f) return;

        Heal(lifeStealAmount);
    }
    
    //증강시스템
    public void ApplyAugmentation(AugmentationSystem aug)
    {
        switch (aug.effectType)
        {
            case AugmentationSystem.AugmentEffectType.BulletDamage:
                weaponSynergy *= aug.value;
                Debug.Log($"무기 공격 시너지 증가 : {weaponSynergy}");
                break;

            case AugmentationSystem.AugmentEffectType.ItemDamage:
                itemSynergy *= aug.value;
                Debug.Log($"아이템 공격 시너지 증가 : {itemSynergy}");
                break;

            case AugmentationSystem.AugmentEffectType.BulletCount:
                bulletPerShot += Mathf.RoundToInt(aug.value);
                Debug.Log($"발사 개수 증가 : {bulletPerShot}");
                break;

            case AugmentationSystem.AugmentEffectType.AttackSpeed:
                attackPerSecond += aug.value;
                Debug.Log($"공격속도 증가, 현재 발사 간격 : {attackPerSecond}");
                break;

            case AugmentationSystem.AugmentEffectType.Shield:
                shieldEnabled = true;
                shieldReady = true;
                shieldInterval = aug.duration > 0f ? aug.duration : 30f;

                if (shieldCoroutine != null)
                    StopCoroutine(shieldCoroutine);

                shieldCoroutine = StartCoroutine(ShieldRoutine());
                Debug.Log($"보호막 활성화 : {shieldInterval}초마다 재충전");
                break;

            case AugmentationSystem.AugmentEffectType.MaxHP:
                maxHp += Mathf.RoundToInt(aug.value);
                hp += Mathf.RoundToInt(aug.value);
                hp = Mathf.Clamp(hp, 0, maxHp);
                Debug.Log($"최대 체력 증가 : {maxHp}");
                break;

            case AugmentationSystem.AugmentEffectType.HPRegen:
                hpRegenAmount += Mathf.RoundToInt(aug.value);
                hpRegenInterval = aug.duration > 0f ? aug.duration : 10f;

                if (hpRegenCoroutine != null)
                    StopCoroutine(hpRegenCoroutine);

                hpRegenCoroutine = StartCoroutine(HPRegenRoutine());
                Debug.Log($"체력 재생 활성화 : {hpRegenInterval}초마다 {hpRegenAmount} 회복");
                break;

            case AugmentationSystem.AugmentEffectType.CharacterScale:
                transform.localScale *= aug.value;
                Debug.Log("캐릭터 크기 변경");
                break;

            case AugmentationSystem.AugmentEffectType.IgnoreObstacle:
                ignoreObstacle = true;
                int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);

                if (obstacleLayer != -1)
                {
                    Physics2D.IgnoreLayerCollision(gameObject.layer, obstacleLayer, true);
                }

                Debug.Log("장애물 충돌 무시 적용");
                break;

            case AugmentationSystem.AugmentEffectType.RemoveSightBlock:
                removeSightBlock = true;
                OnRemoveSightBlock?.Invoke();
                Debug.Log("시야 방해 제거 이벤트 호출");
                break;

            case AugmentationSystem.AugmentEffectType.MoveSpeed:
                moveSpeed += aug.value;
                Debug.Log($"이동속도 증가 : {moveSpeed}");
                break;

            case AugmentationSystem.AugmentEffectType.LifeSteal:
                lifeStealAmount += aug.value;
                Debug.Log($"흡혈 증가 : {lifeStealAmount}");
                break;
        }
    }

    public void ApplyItem(Item item)
    {
        if (item == null) return;
        itemDamage = item.damage;
        moveSpeed = baseMoveSpeed + item.moveSpeed;
        attackPerSecond = baseAttackPerSecond + item.bulletRate;
        maxHp = baseMaxHP + item.hp;
        hp = Mathf.Clamp(Hp, 0, maxHp);

        currentItem = item;
    }
    public void EquipItem(Item newItem)
    {
        if (newItem == null) return;

        ApplyItem(newItem);
    }
    IEnumerator FireRate()
    {
        IsAttack = true;
        yield return new WaitForSeconds(1f/ attackPerSecond);
        IsAttack = false;
        
    }
    //기본 공격총알 장전
    /*IEnumerator Reload()
    {
        isReloading = true;
        PlayerUIManager.Instance.StartCoroutine(PlayerUIManager.Instance.ReloadingText());
        yield return new WaitForSeconds(reloadTime);
        amount = maxAmount;
        isReloading = false;
    }*/
    
    IEnumerator ShieldRoutine()
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

    IEnumerator HPRegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(hpRegenInterval);

            if (hp > 0 && hp < maxHp)
            {
                Heal(hpRegenAmount);
            }
        }
    }
    
}
