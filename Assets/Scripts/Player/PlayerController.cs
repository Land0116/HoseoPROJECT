using System;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;


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
    [SerializeField] private int hp; //playerHP
    [SerializeField] private int maxHp = 50; //playerMaxHP
    [SerializeField] private int gold; //playerGold
    [SerializeField] private float moveSpeed = 3.0f; //playerMoveSpeed;
    [SerializeField] private float attackDamage = 10; //playerDamage;
    [SerializeField] private int amount;
    [SerializeField] private int maxAmount = 5;
    
    
    
    [Header("플레이어의 상태")] 
    [SerializeField] private PlayerState playerState = PlayerState.Idle;
    [SerializeField] private AttackType playerAttackType = AttackType.Base;
    [SerializeField] private float fireRate = 1.0f; //발사 주기
    [SerializeField] private bool isAttack = false; //발사 쿨타임?
    [SerializeField] private float reloadTime = 1.5f; // 장전 걸리는 시간
    [SerializeField] private bool isReloading = false ;
    [SerializeField] private bool isFireInput = false; //발사입력
    
    [Header("데미지 계산 변수")]

// 무기 기본 공격력 (기존 attackDamage를 무기 공격력으로 사용)
    [SerializeField] private float weaponDamage;
// 아이템 공격력
    [SerializeField] private float itemDamage = 0f;
// 무기 증강 시너지
    [SerializeField] private float weaponSynergy = 1f;
// 아이템 증강 시너지
    [SerializeField] private float itemSynergy = 1f;
    
    
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

    public int Amount
    {
        get => amount;
        set => amount = Mathf.Clamp(value,0,maxAmount);
    }
    public int MaxAmount => maxAmount;

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
        get => fireRate;
        set => fireRate = Mathf.Clamp(value, 0, 1);
    }
    public bool IsAttack
    {
        get => isAttack;
        set => isAttack = value;
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
        
        Hp = maxHp;
        Amount = maxAmount;
        weaponDamage = AttackDamage;
    }
    

    void Start()
    {
        if (hideSystemCursor) Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerMouseMovement();
        
    }


    void FixedUpdate()
    {
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
                if (isFireInput && !IsAttack && amount > 0 && !isReloading)
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
                if (isFireInput && !IsAttack && amount > 0 && !isReloading)
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
        }
    }
    

    void PlayerMouseMovement()
    {
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
        if (!IsAttack && playerAttackType == AttackType.Base && amount > 0)
        {
            GameObject bullet = Instantiate(bulletPrefab, gunTip.position, playerBody.rotation);
            ButtonSpawn bulletScript = bullet.GetComponent<ButtonSpawn>();

            if (bulletScript != null)
            {
                bulletScript.SetDamage(GetFinalDamage());
            }
            amount--;
            StartCoroutine(FireRate());
        }
    }
    
    //유니티 기본 InputSystem
    private void OnMove(InputValue movementValue)
    {
        inputDirection = movementValue.Get<Vector2>();
        if (inputDirection.sqrMagnitude > 0 ) { playerState = PlayerState.Walk; }
        else { playerState = PlayerState.Idle; }
    }
    private void OnAttack(InputValue value)
    {
        isFireInput = value.isPressed;
    }

    public void OnDamage(float damage)
    {
        hp -= (int)damage;
        playerState = PlayerState.Hit;
        
        if (hp <= 0)
        {
            Death();
        } 
        //Debug.Log(hp);
    }
    
    public float GetFinalDamage()
    {
        float finalDamage =
            (weaponDamage * weaponSynergy) +
            (itemDamage * itemSynergy);

        return Mathf.RoundToInt(finalDamage);
    }
    public void Death()
    {
        //죽는 애니메이션 후 끝나면 리스폰.
    }

    private void OnReload(InputValue value)
    {
        if (value.isPressed && !isReloading && Amount != MaxAmount)
        {
            StartCoroutine(Reload());
        }
        else if (isReloading)
        {
            Debug.Log("장전 중입니다.");
        }
    }
    
    //증강시스템
    public void ApplyAugmentation(AugmentationSystem aug)
    {
        switch (aug.effectType)
        {
            case AugmentationSystem.AugmentEffectType.BulletDamage:
                // 무기 공격 시너지 증가
                weaponSynergy *= aug.value;
                Debug.Log($"무기 공격 시너지 증가 : {weaponSynergy}");
                break;


            case AugmentationSystem.AugmentEffectType.ItemDamage:
                // 아이템 공격 시너지 증가
                itemSynergy *= aug.value;
                Debug.Log($"아이템 공격 시너지 증가 : {itemSynergy}");
                break;


            case AugmentationSystem.AugmentEffectType.BulletCount:
                // 총알 발사 개수 증가
                maxAmount += (int)aug.value;
                amount = Mathf.Clamp(amount, 0, maxAmount);
                Debug.Log($"총알 개수 증가 : {maxAmount}");
                break;


            case AugmentationSystem.AugmentEffectType.AttackSpeed:
                // 발사 간격 감소 = 공격속도 증가
                FireRateTime = fireRate - aug.value;
                Debug.Log($"공격속도 증가 : {FireRateTime}");
                break;


            // -------------------------
            // 수비형 증강
            // -------------------------

            case AugmentationSystem.AugmentEffectType.Shield:
                Debug.Log("보호막 시스템 필요 (추후 구현)");
                break;


            case AugmentationSystem.AugmentEffectType.MaxHP:
                maxHp += (int)aug.value;
                hp += (int)aug.value;
                hp = Mathf.Clamp(hp, 0, maxHp);
                Debug.Log($"최대 체력 증가 : {maxHp}");
                break;


            case AugmentationSystem.AugmentEffectType.HPRegen:
                Debug.Log("체력 재생 시스템 필요 (추후 구현)");
                break;

            // -------------------------
            // 유틸형 증강
            // -------------------------

            case AugmentationSystem.AugmentEffectType.CharacterScale:
                transform.localScale *= aug.value;
                Debug.Log("캐릭터 크기 감소");
                break;
            
            case AugmentationSystem.AugmentEffectType.IgnoreObstacle:
                Debug.Log("장애물 충돌 무시 (레이어 처리 필요)");
                break;

            case AugmentationSystem.AugmentEffectType.RemoveSightBlock:
                Debug.Log("시야 방해 제거 (오브젝트 제어 필요)");
                break;


            case AugmentationSystem.AugmentEffectType.MoveSpeed:
                moveSpeed += aug.value;
                Debug.Log($"이동속도 증가 : {moveSpeed}");
                break;


            case AugmentationSystem.AugmentEffectType.LifeSteal:
                Debug.Log("흡혈 기능 (적중 시 체력 회복 구현 필요)");
                break;
        }
    }
    
    IEnumerator FireRate()
    {
        IsAttack = true;
        yield return new WaitForSeconds(FireRateTime);
        IsAttack = false;
        
    }
    //기본 공격총알 장전
    IEnumerator Reload()
    {
        isReloading = true;
        PlayerUIManager.Instance.StartCoroutine(PlayerUIManager.Instance.ReloadingText());
        yield return new WaitForSeconds(reloadTime);
        amount = maxAmount;
        isReloading = false;
    }
    
}
