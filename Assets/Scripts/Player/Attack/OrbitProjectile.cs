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
        float newLifeTime)
    {
        owner = newOwner;
        currentAngle = startAngle;
        radius = newRadius;
        angularSpeed = newAngularSpeed;
        damage = newDamage;
        hitInterval = Mathf.Max(0.01f, newHitInterval);
        lifeTime = newLifeTime;

        if (lifeTime > 0f)
        {
            destroyTime = Time.time + lifeTime;
        }
        else
        {
            destroyTime = -1f;
        }
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
        // 플레이어가 사라졌으면 주위탄도 같이 제거
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // 수명 제한이 있으면 시간 지나면 제거
        if (destroyTime > 0f && Time.time >= destroyTime)
        {
            Destroy(gameObject);
            return;
        }

        // 각도 갱신
        currentAngle += angularSpeed * Time.deltaTime;

        // owner 기준 원운동 위치 계산
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

        transform.position = owner.position + (Vector3)offset;

        // 주위탄의 바깥 방향으로 회전 보정 (원하면 비주얼용)
        if (offset.sqrMagnitude > 0.0001f)
        {
            transform.right = offset.normalized;
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