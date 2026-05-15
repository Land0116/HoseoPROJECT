using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본 투사체 스크립트
/// 
/// 역할:
/// 1. 앞으로 이동
/// 2. 유도
/// 3. 관통
/// 4. 벽 반사
/// 5. 폭발
/// 6. 연쇄 공격
/// 7. 화상(DoT) 적용
/// 8. owner(발사자)에게는 절대 피해를 주지 않음
/// 
/// 전제:
/// - 적은 IDamageable 구현
/// - 적 충돌은 Trigger 권장
/// - 벽 반사는 Collision 권장
/// - 총알의 전방은 transform.right 기준
/// </summary>
public class ButtonSpawn : MonoBehaviour
{
    [Header("기본 이동")] [SerializeField] private float speed = 25f;
    [SerializeField] private float baseLifeTime = 1f;

    [Header("디버그 / 런타임 값")] [SerializeField]
    private float damage = 1f;

    [SerializeField] private float projectileLifeMultiplier = 1f;

    [Header("유도")] [SerializeField] private float homingStrength = 0f;
    [SerializeField] private float homingDuration = 0f;
    [SerializeField] private float homingSearchRadius = 8f;

    [Header("반사 / 관통")] [SerializeField] private int bounceCount = 0;
    [SerializeField] private int pierceCount = 0;

    [Header("폭발")] [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private float explosionDamageMultiplier = 1f;

    [Header("연쇄")] [SerializeField] private int chainCount = 0;
    [SerializeField] private float chainRange = 0f;


    [Header("도트")] [SerializeField] private float dotDamagePerSecond = 0f;
    [SerializeField] private float dotDuration = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float destroyTime = -1f;
    private float homingEndTime = -1f;

    private int remainingBounceCount;
    private int remainingPierceCount;
    private int remainingChainCount;

    private bool isDestroyed = false;

    // 이미 맞은 적 기록
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    [Header("연쇄 총알 생성")] [SerializeField] private GameObject chainProjectilePrefab;

    private Transform lockedHomingTarget;

    private bool runtimeInitialized = false;

    // -----------------------------
    // owner 관련
    // -----------------------------
    private Transform ownerRoot;
    private Collider2D[] ownerColliders;
    private Collider2D[] myColliders;

    [Header("타겟 필터")] 
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask wallLayerMask;

// NonAlloc 버퍼
    private static readonly Collider2D[] targetBuffer = new Collider2D[32];
    private static readonly Collider2D[] hitBuffer = new Collider2D[32];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        if (runtimeInitialized) return;

        InitializeNormalProjectile();
    }

    private void InitializeNormalProjectile()
    {
        runtimeInitialized = true;

        moveDirection = transform.right.normalized;

        remainingBounceCount = bounceCount;
        remainingPierceCount = pierceCount;
        remainingChainCount = chainCount;

        if (homingDuration > 0f)
            homingEndTime = Time.time + homingDuration;
        else
            homingEndTime = -1f;

        float finalLifeTime = Mathf.Max(0.05f, baseLifeTime * projectileLifeMultiplier);
        destroyTime = Time.time + finalLifeTime;

        // 생성 시 1번만 유도 타겟 Lock
        LockInitialHomingTarget();
    }

    private void LockInitialHomingTarget()
    {
        lockedHomingTarget = null;

        if (homingStrength <= 0f) return;

        lockedHomingTarget = FindClosestTarget();

        if (lockedHomingTarget == null) return;

        Vector2 dir = (Vector2)lockedHomingTarget.position - (Vector2)transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            moveDirection = dir.normalized;
            transform.right = moveDirection;
        }
    }

    public void SetLockedHomingTarget(Transform target)
    {
        lockedHomingTarget = target;

        if (lockedHomingTarget == null) return;

        Vector2 dir = (Vector2)lockedHomingTarget.position - (Vector2)transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            moveDirection = dir.normalized;
            transform.right = moveDirection;
        }
    }

    private void Update()
    {
        if (isDestroyed) return;

        if (Time.time >= destroyTime)
        {
            DestroyProjectile();
            return;
        }

        UpdateHoming();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (isDestroyed) return;

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
        else
        {
            transform.position += (Vector3)(moveDirection * speed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 발사자(owner) 설정
    /// 
    /// 역할:
    /// 1. 총알이 발사자를 절대 때리지 않게 함
    /// 2. 발사자와 총알 콜라이더 충돌을 물리적으로 무시
    /// </summary>
    public void SetOwner(GameObject ownerObject)
    {
        if (ownerObject == null) return;

        ownerRoot = ownerObject.transform.root;
        ownerColliders = ownerRoot.GetComponentsInChildren<Collider2D>(true);

        if (myColliders == null || myColliders.Length == 0)
            myColliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < myColliders.Length; i++)
        {
            if (myColliders[i] == null) continue;

            for (int j = 0; j < ownerColliders.Length; j++)
            {
                if (ownerColliders[j] == null) continue;
                Physics2D.IgnoreCollision(myColliders[i], ownerColliders[j], true);
            }
        }
    }

    /// <summary>
    /// 지금 닿은 콜라이더가 발사자인지 확인
    /// </summary>
    private bool IsOwnerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (ownerRoot == null) return false;

        return other.transform.root == ownerRoot;
    }

    private void UpdateHoming()
    {
        if (homingStrength <= 0f) return;
        if (homingEndTime > 0f && Time.time > homingEndTime) return;

        // 매 프레임 새 타겟을 찾지 않는다.
        // 생성 시 / 연쇄 시 Lock된 타겟만 따라간다.
        if (lockedHomingTarget == null) return;

        Vector2 toTarget = (Vector2)lockedHomingTarget.position - (Vector2)transform.position;

        if (toTarget.sqrMagnitude <= 0.0001f) return;

        toTarget.Normalize();

        float turnSpeedDegPerSec = homingStrength * 720f;
        float maxRadiansDelta = turnSpeedDegPerSec * Mathf.Deg2Rad * Time.deltaTime;

        Vector3 rotated = Vector3.RotateTowards(moveDirection, toTarget, maxRadiansDelta, 0f);
        moveDirection = ((Vector2)rotated).normalized;
    }

    private void UpdateRotation()
    {
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.right = moveDirection;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        // 발사자와의 충돌은 무조건 무시
        if (IsOwnerCollider(other))
            return;

        // 벽 / 장애물 Trigger 충돌 처리
        if (IsWallLayer(other.gameObject))
        {
            HandleWallTriggerHit(other);
            return;
        }

        // 적 Trigger 충돌 처리
        IDamageable damageable = GetDamageable(other);
        if (damageable != null)
        {
            HandleEnemyHit(other, damageable);
            return;
        }
    }
    private void HandleWallTriggerHit(Collider2D wallCollider)
    {
        if (wallCollider == null) return;

        // Trigger 방식에서는 Collision Contact Normal이 없어서
        // ClosestPoint 기준으로 대략적인 반사 방향을 계산한다.
        if (remainingBounceCount > 0)
        {
            Vector2 closestPoint = wallCollider.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - closestPoint).normalized;

            // 겹쳐 있는 상태라 normal을 못 구하면 현재 이동 방향의 반대로 처리
            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = -moveDirection;
            }

            moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
            remainingBounceCount--;
            return;
        }

        if (explosionRadius > 0f)
        {
            ExplodeAt(transform.position, null);
        }

        DestroyProjectile();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;
        if (collision.contactCount <= 0) return;

        if (IsOwnerCollider(collision.collider))
            return;

        // 적이면 적 처리
        IDamageable damageable = GetDamageable(collision.collider);
        if (damageable != null)
        {
            HandleEnemyHit(collision.collider, damageable);
            return;
        }

        // 벽/장애물만 반사 또는 소멸 처리
        if (!IsWallLayer(collision.collider.gameObject))
            return;

        if (remainingBounceCount > 0)
        {
            Vector2 normal = collision.GetContact(0).normal;
            moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
            remainingBounceCount--;
            return;
        }

        if (explosionRadius > 0f)
        {
            ExplodeAt(collision.GetContact(0).point, null);
            //PlayExplosionVfx(collision.GetContact(0).point, explosionRadius);
        }

        DestroyProjectile();
    }

    private void HandleEnemyHit(Collider2D targetCollider, IDamageable damageable)
    {
        if (targetCollider == null || damageable == null) return;

        // 발사자는 맞지 않음
        if (IsOwnerCollider(targetCollider))
            return;

        // 이미 맞은 적은 다시 맞지 않게 처리
        if (hitTargets.Contains(targetCollider))
            return;

        hitTargets.Add(targetCollider);

        // 직접 피해
        damageable.OnDamage(damage);
        // 실제 준 데미지 기준으로 흡혈 처리
        NotifyOwnerHitEnemy(damage);

        // 2. 화상 / 도트 적용
        if (dotDamagePerSecond > 0f && dotDuration > 0f)
        {
            ProjectileDotReceiver dotReceiver = GetOrCreateDotReceiver(targetCollider, damageable);
            if (dotReceiver != null)
            {
                dotReceiver.ApplyDot(dotDamagePerSecond, dotDuration);
            }
        }

        // 3. 폭발 적용
        if (explosionRadius > 0f)
        {
            ExplodeAt(targetCollider.transform.position, targetCollider);
        }

        // 4. 연쇄탄 처리
        // 기존처럼 주변 적에게 즉시 데미지를 주는 게 아니라,
        // 총알의 이동 방향을 다음 적 방향으로 바꾼다.
        if (remainingChainCount > 0)
        {
            TrySpawnChainProjectile(targetCollider);

            DestroyProjectile();
            return;
        }

        // 5. 관통 처리
        if (remainingPierceCount > 0)
        {
            remainingPierceCount--;

            // 몬스터 Collider가 IsTrigger = false인 경우,
            // 물리 충돌 때문에 총알이 몬스터 몸에 막히거나 밀릴 수 있다.
            // 그래서 이미 맞은 몬스터와의 물리 충돌을 무시시켜 실제로 통과하게 만든다.
            IgnoreTargetCollision(targetCollider, damageable);

            // 관통 가능 횟수를 사용했으므로 총알은 계속 진행한다.
            return;
        }

        // 6. 관통 횟수가 없으면 적을 맞은 뒤 총알 제거
        DestroyProjectile();
    }

    /// <summary>
    /// 관통한 대상과 총알의 물리 충돌을 무시한다.
    /// 
    /// 이유:
    /// - 몬스터 Collider가 IsTrigger = false일 경우
    ///   총알이 데미지는 주지만 실제 물리 충돌 때문에 막힐 수 있다.
    /// - 관통탄은 몬스터를 뚫고 지나가야 하므로,
    ///   이미 맞은 몬스터의 Collider들과 총알 Collider들의 충돌을 무시한다.
    /// </summary>
    private void IgnoreTargetCollision(Collider2D targetCollider, IDamageable damageable)
    {
        if (targetCollider == null) return;

        if (myColliders == null || myColliders.Length == 0)
            myColliders = GetComponentsInChildren<Collider2D>(true);

        Collider2D[] targetColliders = null;

        // IDamageable이 MonoBehaviour라면 그 오브젝트 기준으로 자식 Collider까지 전부 가져온다.
        // 적이 자식 콜라이더 여러 개를 가지고 있어도 모두 무시하기 위함.
        if (damageable is Component damageComponent)
        {
            targetColliders = damageComponent.GetComponentsInChildren<Collider2D>(true);
        }

        // fallback: targetCollider 하나만 사용
        if (targetColliders == null || targetColliders.Length == 0)
        {
            targetColliders = new Collider2D[] { targetCollider };
        }

        for (int i = 0; i < myColliders.Length; i++)
        {
            Collider2D myCol = myColliders[i];
            if (myCol == null) continue;

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider2D targetCol = targetColliders[j];
                if (targetCol == null) continue;

                Physics2D.IgnoreCollision(myCol, targetCol, true);
            }
        }
    }

    private bool TrySpawnChainProjectile(Collider2D originTarget)
    {
        if (remainingChainCount <= 0) return false;
        if (chainRange <= 0f) return false;
        if (originTarget == null) return false;

        Collider2D nextTarget = FindClosestChainTarget(originTarget);

        if (nextTarget == null) return false;

        GameObject prefab = chainProjectilePrefab != null
            ? chainProjectilePrefab
            : gameObject;

        GameObject obj = Instantiate(
            prefab,
            transform.position,
            Quaternion.identity
        );

        ButtonSpawn chainProjectile = obj.GetComponent<ButtonSpawn>();

        if (chainProjectile == null)
        {
            Destroy(obj);
            return false;
        }

        chainProjectile.InitializeChainProjectile(
            this,
            nextTarget.transform,
            remainingChainCount - 1,
            hitTargets,
            originTarget
        );

        return true;
    }

    private Collider2D FindClosestChainTarget(Collider2D originTarget)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            originTarget.transform.position,
            chainRange,
            hitBuffer,
            enemyLayerMask
        );

        IDamageable originDamageable = GetDamageable(originTarget);
        Component originComponent = originDamageable as Component;

        Collider2D closestTarget = null;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = hitBuffer[i];

            if (candidate == null) continue;
            if (candidate == originTarget) continue;
            if (IsOwnerCollider(candidate)) continue;
            if (hitTargets.Contains(candidate)) continue;

            IDamageable candidateDamageable = GetDamageable(candidate);
            if (candidateDamageable == null) continue;

            // 같은 IDamageable이면 제외
            if (ReferenceEquals(candidateDamageable, originDamageable))
                continue;

            // 같은 몬스터 루트면 제외
            if (originComponent != null && candidateDamageable is Component candidateComponent)
            {
                if (candidateComponent.transform.root == originComponent.transform.root)
                    continue;
            }

            float sqr = ((Vector2)candidate.transform.position - (Vector2)originTarget.transform.position).sqrMagnitude;

            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closestTarget = candidate;
            }
        }

        return closestTarget;
    }

    private ProjectileDotReceiver GetOrCreateDotReceiver(Collider2D targetCollider, IDamageable damageable)
    {
        if (targetCollider == null) return null;

        // IDamageable이 붙어있는 본체 오브젝트에 DotReceiver를 붙이는 쪽이 안전하다.
        // 자식 콜라이더마다 따로 DotReceiver가 붙는 문제를 막기 위함.
        if (damageable is Component damageComponent)
        {
            ProjectileDotReceiver receiver = damageComponent.GetComponent<ProjectileDotReceiver>();

            if (receiver == null)
            {
                receiver = damageComponent.gameObject.AddComponent<ProjectileDotReceiver>();
            }

            return receiver;
        }

        ProjectileDotReceiver fallback = targetCollider.GetComponent<ProjectileDotReceiver>();

        if (fallback == null)
        {
            fallback = targetCollider.gameObject.AddComponent<ProjectileDotReceiver>();
        }

        return fallback;
    }

    private void ExplodeAt(Vector2 center, Collider2D excludeTarget)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            if (hit == excludeTarget) continue;
            if (IsOwnerCollider(hit)) continue;

            IDamageable damageable = GetDamageable(hit);
            if (damageable == null) continue;

            float explosionDamage = damage * explosionDamageMultiplier;

            damageable.OnDamage(explosionDamage);

            // 폭발 피해도 적중으로 인정해서 흡혈 처리
            NotifyOwnerHitEnemy(explosionDamage);
        }
    }

    private Transform FindClosestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            homingSearchRadius,
            targetBuffer,
            enemyLayerMask
        );

        float closestSqr = float.MaxValue;
        Transform closestTarget = null;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = targetBuffer[i];
            if (col == null) continue;
            if (IsOwnerCollider(col)) continue;

            IDamageable damageable = GetDamageable(col);
            if (damageable == null) continue;

            float sqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closestTarget = col.transform;
            }
        }

        return closestTarget;
    }

    private IDamageable GetDamageable(Collider2D col)
    {
        if (col == null) return null;

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable != null) return damageable;

        return col.GetComponentInParent<IDamageable>();
    }

    private void DestroyProjectile()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        Destroy(gameObject);
    }

    private bool IsWallLayer(GameObject target)
    {
        return ((1 << target.layer) & wallLayerMask) != 0;
    }

    private void NotifyOwnerHitEnemy(float dealtDamage)
    {
        if (dealtDamage <= 0f) return;

        PlayerController player = null;

        if (ownerRoot != null)
        {
            player = ownerRoot.GetComponentInChildren<PlayerController>();
        }

        if (player == null)
        {
            player = PlayerController.Instance;
        }

        if (player != null)
        {
            player.OnHitEnemy(dealtDamage);
        }
    }

    #region 연쇄탄 초기화함수
    private void InitializeChainProjectile(
        ButtonSpawn source,
        Transform chainTarget,
        int newRemainingChainCount,
        HashSet<Collider2D> inheritedHitTargets,
        Collider2D originTarget)
    {
        runtimeInitialized = true;

        rb = GetComponent<Rigidbody2D>();
        myColliders = GetComponentsInChildren<Collider2D>(true);

        hitTargets = new HashSet<Collider2D>();

        if (inheritedHitTargets != null)
        {
            foreach (Collider2D hit in inheritedHitTargets)
            {
                if (hit == null) continue;
                hitTargets.Add(hit);
            }
        }

        if (originTarget != null)
            hitTargets.Add(originTarget);

        damage = source.damage;
        projectileLifeMultiplier = source.projectileLifeMultiplier;

        homingStrength = source.homingStrength;
        homingDuration = source.homingDuration;
        homingSearchRadius = source.homingSearchRadius;

        bounceCount = source.bounceCount;
        pierceCount = source.pierceCount;

        explosionRadius = source.explosionRadius;
        explosionDamageMultiplier = source.explosionDamageMultiplier;

        chainCount = source.chainCount;
        chainRange = source.chainRange;

        dotDamagePerSecond = source.dotDamagePerSecond;
        dotDuration = source.dotDuration;

        enemyLayerMask = source.enemyLayerMask;
        wallLayerMask = source.wallLayerMask;

        remainingBounceCount = source.remainingBounceCount;
        remainingPierceCount = source.remainingPierceCount;
        remainingChainCount = Mathf.Max(0, newRemainingChainCount);

        ownerRoot = source.ownerRoot;

        if (ownerRoot != null)
        {
            SetOwner(ownerRoot.gameObject);
        }

        lockedHomingTarget = chainTarget;

        if (lockedHomingTarget != null)
        {
            Vector2 dir = (Vector2)lockedHomingTarget.position - (Vector2)transform.position;

            if (dir.sqrMagnitude > 0.0001f)
            {
                moveDirection = dir.normalized;
                transform.right = moveDirection;
            }
            else
            {
                moveDirection = source.moveDirection;
            }
        }
        else
        {
            moveDirection = source.moveDirection;
        }

        if (homingDuration > 0f)
            homingEndTime = Time.time + homingDuration;
        else
            homingEndTime = -1f;

        float chainLifeTime = chainRange / Mathf.Max(1f, speed) + 0.25f;
        float normalLifeTime = Mathf.Max(0.05f, baseLifeTime * projectileLifeMultiplier);

        destroyTime = Time.time + Mathf.Max(chainLifeTime, normalLifeTime);

        if (originTarget != null)
        {
            IDamageable originDamageable = GetDamageable(originTarget);
            IgnoreTargetCollision(originTarget, originDamageable);
        }
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
    }
    #endregion
    
    #region PlayerController Setter 연결 함수

    public void SetDamage(float value)
    {
        damage = value;
    }

    public void SetProjectileLifeMultiplier(float value)
    {
        projectileLifeMultiplier = Mathf.Max(0.05f, value);
    }

    public void SetHoming(float strength, float duration)
    {
        homingStrength = Mathf.Max(0f, strength);
        homingDuration = Mathf.Max(0f, duration);
        
        if (runtimeInitialized)
        {
            if (homingDuration > 0f)
                homingEndTime = Time.time + homingDuration;
            else
                homingEndTime = -1f;

            LockInitialHomingTarget();
        }
    }

    public void SetBounce(int value)
    {
        bounceCount = Mathf.Max(0, value);
        remainingBounceCount = bounceCount;
    }

    public void SetPierce(int value)
    {
        pierceCount = Mathf.Max(0, value);
        remainingPierceCount = pierceCount;
    }

    public void SetExplosion(float radiusValue, float damageMultiplierValue)
    {
        explosionRadius = Mathf.Max(0f, radiusValue);
        explosionDamageMultiplier = Mathf.Max(0f, damageMultiplierValue);
    }

    public void SetChain(int count, float range)
    {
        chainCount = Mathf.Max(0, count);
        chainRange = Mathf.Max(0f, range);

        remainingChainCount = chainCount;
    }

    public void SetDot(float dps, float duration)
    {
        dotDamagePerSecond = Mathf.Max(0f, dps);
        dotDuration = Mathf.Max(0f, duration);
    }

    #endregion
}