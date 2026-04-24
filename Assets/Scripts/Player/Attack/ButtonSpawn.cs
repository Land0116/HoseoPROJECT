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
    [Header("기본 이동")]
    [SerializeField] private float speed = 25f;
    [SerializeField] private float baseLifeTime = 1f;

    [Header("디버그 / 런타임 값")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float projectileLifeMultiplier = 1f;

    [Header("유도")]
    [SerializeField] private float homingStrength = 0f;
    [SerializeField] private float homingDuration = 0f;
    [SerializeField] private float homingSearchRadius = 8f;

    [Header("반사 / 관통")]
    [SerializeField] private int bounceCount = 0;
    [SerializeField] private int pierceCount = 0;

    [Header("폭발")]
    [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private float explosionDamageMultiplier = 1f;

    [Header("연쇄")]
    [SerializeField] private int chainCount = 0;
    [SerializeField] private float chainRange = 0f;
    

    [Header("도트")]
    [SerializeField] private float dotDamagePerSecond = 0f;
    [SerializeField] private float dotDuration = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float destroyTime = -1f;
    private float homingEndTime = -1f;

    private int remainingBounceCount;
    private int remainingPierceCount;

    private bool isDestroyed = false;

    // 이미 맞은 적 기록
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

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
        // 총알 프리팹의 앞 방향을 transform.right로 가정
        moveDirection = transform.right.normalized;

        remainingBounceCount = bounceCount;
        remainingPierceCount = pierceCount;

        if (homingDuration > 0f)
            homingEndTime = Time.time + homingDuration;
        else
            homingEndTime = -1f;

        float finalLifeTime = Mathf.Max(0.05f, baseLifeTime * projectileLifeMultiplier);
        destroyTime = Time.time + finalLifeTime;
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

        Transform target = FindClosestTarget();
        if (target == null) return;

        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // homingStrength를 "회전 강도 계수"로 사용
        // 0.3 / 0.6 / 0.9 같은 수치도 체감되도록 증폭
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

        // -----------------------------
        // 발사자와의 충돌은 무조건 무시
        // -----------------------------
        if (IsOwnerCollider(other))
            return;

        IDamageable damageable = GetDamageable(other);
        if (damageable != null)
        {
            HandleEnemyHit(other, damageable);
        }
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

        // owner는 맞지 않음
        if (IsOwnerCollider(targetCollider))
            return;

        if (hitTargets.Contains(targetCollider))
            return;

        hitTargets.Add(targetCollider);

        damageable.OnDamage(damage);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHitEnemy();
        }

        if (dotDamagePerSecond > 0f && dotDuration > 0f)
        {
            ProjectileDotReceiver dotReceiver = targetCollider.GetComponent<ProjectileDotReceiver>();
            if (dotReceiver == null)
            {
                dotReceiver = targetCollider.gameObject.AddComponent<ProjectileDotReceiver>();
            }

            dotReceiver.ApplyDot(dotDamagePerSecond, dotDuration);
        }

        if (chainCount > 0 && chainRange > 0f)
        {
            ApplyChainDamage(targetCollider);
        }

        if (explosionRadius > 0f)
        {
            ExplodeAt(targetCollider.transform.position, targetCollider);
        }

        if (remainingPierceCount > 0)
        {
            remainingPierceCount--;

            // 마지막 허용 관통을 다 썼으면 여기서 종료
            if (remainingPierceCount <= 0)
            {
                DestroyProjectile();
            }
            return;
        }
        DestroyProjectile();
    }

    private void ApplyChainDamage(Collider2D originTarget)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(originTarget.transform.position, chainRange);

        int appliedCount = 0;

        for (int i = 0; i < nearby.Length; i++)
        {
            Collider2D candidate = nearby[i];
            if (candidate == null) continue;
            if (candidate == originTarget) continue;
            if (IsOwnerCollider(candidate)) continue;

            IDamageable damageable = GetDamageable(candidate);
            if (damageable == null) continue;
            if (hitTargets.Contains(candidate)) continue;

            damageable.OnDamage(damage);
            hitTargets.Add(candidate);
            appliedCount++;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnHitEnemy();
            }

            if (appliedCount >= chainCount)
                break;
        }
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

            damageable.OnDamage(damage * explosionDamageMultiplier);
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
    }

    public void SetDot(float dps, float duration)
    {
        dotDamagePerSecond = Mathf.Max(0f, dps);
        dotDuration = Mathf.Max(0f, duration);
    }

    #endregion
}