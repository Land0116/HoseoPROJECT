using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주위탄 전용 스크립트
/// 
/// 역할:
/// 1. owner(플레이어)를 기준으로 계속 회전한다.
/// 2. 충돌한 적에게 일정 간격마다 데미지를 준다.
/// 3. 선택적으로 폭발 / 도트 효과도 같이 적용할 수 있다.
/// 
/// 사용 방법:
/// - 주위탄 프리팹에 이 스크립트를 붙인다.
/// - Collider2D는 Trigger 체크 권장
/// - Rigidbody2D는 Kinematic 권장
/// </summary>
public class OrbitProjectile : MonoBehaviour
{
    [Header("기본 런타임 값")]
    [SerializeField] private Transform owner;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float angularSpeed = 180f;
    [SerializeField] private float currentAngle;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float hitInterval = 0.2f;
    [SerializeField] private float lifeTime = 0f;

    [Header("선택적 추가 효과")]
    [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private float explosionDamageMultiplier = 1f;
    [SerializeField] private float dotDamagePerSecond = 0f;
    [SerializeField] private float dotDuration = 0f;
    
    [Header("편대 배치")]
    [SerializeField] private Vector2 formationOffset = Vector2.zero;

    [Header("주위탄 자동공격")]
    [SerializeField] private bool autoAttackEnabled = false;
    [SerializeField] private float autoAttackRange = 4f;
    [SerializeField] private float autoAttackCooldown = 1f;
    [SerializeField] private float autoAttackMoveSpeed = 12f;
    [SerializeField] private LayerMask autoAttackEnemyLayerMask;

    private float nextAutoAttackTime = -1f;
    private Transform autoAttackTarget;
    private bool isAutoAttacking = false;
    private bool isReturningToOrbit = false;

    private static readonly Collider2D[] autoAttackBuffer = new Collider2D[32];
    
    
    private Transform ownerRoot;
    private Collider2D[] ownerColliders;
    private Collider2D[] myColliders;
    private float destroyTime = -1f;

    // 같은 적을 프레임마다 계속 때리지 않도록, 적별 다음 타격 가능 시간 기록
    private readonly Dictionary<Collider2D, float> nextHitTimeByTarget = new Dictionary<Collider2D, float>();

    /// <summary>
    /// PlayerController가 생성 직후 호출해서 초기값을 넣는다.
    /// </summary>
    public void Initialize(
        Transform newOwner,
        float startAngle,
        float newRadius,
        float newAngularSpeed,
        float newDamage,
        float newHitInterval,
        float newLifeTime,
        Vector2 newFormationOffset)
    {
        owner = newOwner;
        currentAngle = startAngle;
        radius = newRadius;
        angularSpeed = newAngularSpeed;
        damage = newDamage;
        hitInterval = Mathf.Max(0.01f, newHitInterval);
        lifeTime = newLifeTime;
        formationOffset = newFormationOffset;

        if (lifeTime > 0f)
            destroyTime = Time.time + lifeTime;
        else
            destroyTime = -1f;
    }

    public void SetExplosion(float radiusValue, float damageMultiplierValue)
    {
        explosionRadius = radiusValue;
        explosionDamageMultiplier = Mathf.Max(0f, damageMultiplierValue);
    }

    public void SetDot(float dps, float duration)
    {
        dotDamagePerSecond = dps;
        dotDuration = duration;
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        if (destroyTime > 0f && Time.time >= destroyTime)
        {
            Destroy(gameObject);
            return;
        }

        currentAngle += angularSpeed * Time.deltaTime;

        Vector2 orbitPosition = GetOrbitWorldPosition();

        if (autoAttackEnabled)
        {
            UpdateAutoAttack(orbitPosition);
            return;
        }

        transform.position = orbitPosition;
        UpdateVisualDirection(orbitPosition);
    }
    
    /// <summary>
    /// 현재 주위탄이 있어야 할 원래 궤도 위치를 계산한다.
    /// </summary>
    private Vector2 GetOrbitWorldPosition()
    {
        float rad = currentAngle * Mathf.Deg2Rad;

        Vector2 radialDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        Vector2 tangentDir = new Vector2(-radialDir.y, radialDir.x).normalized;

        Vector2 orbitOffset =
            radialDir * radius +
            tangentDir * formationOffset.x +
            radialDir * formationOffset.y;

        return (Vector2)owner.position + orbitOffset;
    }
    /// <summary>
    /// 주위탄 자동공격 처리.
    /// 
    /// 흐름:
    /// 1. 평소에는 원래 궤도 위치를 따라 돈다.
    /// 2. 쿨타임이 끝났고 범위 안에 적이 있으면 적에게 날아간다.
    /// 3. 적을 때리거나 대상이 사라지면 원래 궤도로 복귀한다.
    /// </summary>
    private void UpdateAutoAttack(Vector2 orbitPosition)
    {
        if (!isAutoAttacking && !isReturningToOrbit)
        {
            transform.position = orbitPosition;
            UpdateVisualDirection(orbitPosition);

            if (Time.time >= nextAutoAttackTime)
            {
                autoAttackTarget = FindClosestAutoAttackTarget();

                if (autoAttackTarget != null)
                {
                    isAutoAttacking = true;
                    nextAutoAttackTime = Time.time + autoAttackCooldown;
                }
            }

            return;
        }

        if (isAutoAttacking)
        {
            if (autoAttackTarget == null)
            {
                isAutoAttacking = false;
                isReturningToOrbit = true;
                return;
            }

            Vector2 targetPos = autoAttackTarget.position;

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPos,
                autoAttackMoveSpeed * Time.deltaTime
            );

            Vector2 dir = targetPos - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.right = dir.normalized;

            if (Vector2.Distance(transform.position, targetPos) <= 0.1f)
            {
                isAutoAttacking = false;
                isReturningToOrbit = true;
            }

            return;
        }

        if (isReturningToOrbit)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                orbitPosition,
                autoAttackMoveSpeed * Time.deltaTime
            );

            Vector2 dir = orbitPosition - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.right = dir.normalized;

            if (Vector2.Distance(transform.position, orbitPosition) <= 0.05f)
            {
                isReturningToOrbit = false;
            }
        }
    }
    
    /// <summary>
    /// 자동공격 범위 안에서 가장 가까운 적을 찾는다.
    /// NonAlloc 사용으로 GC 발생을 줄인다.
    /// </summary>
    private Transform FindClosestAutoAttackTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            autoAttackRange,
            autoAttackBuffer,
            autoAttackEnemyLayerMask
        );

        Transform closest = null;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = autoAttackBuffer[i];
            if (col == null) continue;
            if (IsOwnerCollider(col)) continue;

            IDamageable damageable = GetDamageable(col);
            if (damageable == null) continue;

            float sqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = col.transform;
            }
        }

        return closest;
    }
    
    private void UpdateVisualDirection(Vector2 orbitPosition)
    {
        Vector2 dir = orbitPosition - (Vector2)owner.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.right = dir.normalized;
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsOwnerCollider(other))
            return;
        TryDamageTarget(other);
    }
    
    public void SetOwner(GameObject ownerObject)
    {
        if (ownerObject == null) return;

        ownerRoot = ownerObject.transform.root;
        ownerColliders = ownerRoot.GetComponentsInChildren<Collider2D>(true);
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

    private bool IsOwnerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (ownerRoot == null) return false;

        return other.transform.root == ownerRoot;
    }

    /// <summary>
    /// 적 충돌 처리
    /// - 일정 간격마다만 타격
    /// - 도트/폭발 부가효과 적용 가능
    /// </summary>
    private void TryDamageTarget(Collider2D other)
    {
        if (other == null) return;

        IDamageable damageable = GetDamageable(other);
        if (damageable == null) return;

        // 같은 적을 너무 자주 때리는 것 방지
        if (nextHitTimeByTarget.TryGetValue(other, out float nextTime))
        {
            if (Time.time < nextTime)
                return;
        }

        nextHitTimeByTarget[other] = Time.time + hitInterval;

        // 본체 데미지
        damageable.OnDamage(damage);

        // 플레이어 흡혈 등 "적 명중" 트리거
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHitEnemy();
        }

        // 도트 부여
        if (dotDamagePerSecond > 0f && dotDuration > 0f)
        {
            ProjectileDotReceiver dotReceiver = other.GetComponent<ProjectileDotReceiver>();
            if (dotReceiver == null)
            {
                dotReceiver = other.gameObject.AddComponent<ProjectileDotReceiver>();
            }

            dotReceiver.ApplyDot(dotDamagePerSecond, dotDuration);
        }

        // 폭발
        if (explosionRadius > 0f)
        {
            ExplodeAt(other.transform.position, other);
        }
    }

    /// <summary>
    /// 폭발 범위 내 적들에게 추가 피해
    /// 직접 맞은 적은 중복 피해 방지를 위해 제외
    /// </summary>
    private void ExplodeAt(Vector2 center, Collider2D directHitTarget)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            if (hit == directHitTarget) continue;

            // owner는 폭발에도 맞지 않게 처리
            if (IsOwnerCollider(hit)) continue;

            IDamageable damageable = GetDamageable(hit);
            if (damageable == null) continue;

            damageable.OnDamage(damage * explosionDamageMultiplier);
        }
    }
    
    /// <summary>
    /// 주위탄 자동공격 설정.
    /// 
    /// 자동연사 패시브가 있을 때만 true가 들어온다.
    /// </summary>
    public void SetAutoAttack(
        bool enabled,
        float range,
        float cooldown,
        float moveSpeed,
        LayerMask enemyLayerMask)
    {
        autoAttackEnabled = enabled;
        autoAttackRange = Mathf.Max(0f, range);
        autoAttackCooldown = Mathf.Max(0.05f, cooldown);
        autoAttackMoveSpeed = Mathf.Max(1f, moveSpeed);
        autoAttackEnemyLayerMask = enemyLayerMask;
    }

    private IDamageable GetDamageable(Collider2D col)
    {
        if (col == null) return null;

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable != null) return damageable;

        return col.GetComponentInParent<IDamageable>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (owner != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, radius);
        }

        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
#endif
}