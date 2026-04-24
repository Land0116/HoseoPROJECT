using System.Collections;
using UnityEngine;

/// <summary>
/// 투사체가 적에게 부여하는 DoT(지속 피해) 전용 컴포넌트
/// 
/// 특징:
/// - 맞은 적에게 런타임으로 자동 부착 가능
/// - 새 도트가 들어오면 기존 도트를 갱신
/// - 매우 복잡한 중첩 규칙 대신 "가장 최근 도트로 갱신" 방식 사용
/// </summary>
public class ProjectileDotReceiver : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.5f;

    private Coroutine dotCoroutine;

    /// <summary>
    /// 도트 적용
    /// </summary>
    public void ApplyDot(float damagePerSecond, float duration)
    {
        if (damagePerSecond <= 0f || duration <= 0f)
            return;

        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
        }

        dotCoroutine = StartCoroutine(DotRoutine(damagePerSecond, duration));
    }

    private IEnumerator DotRoutine(float damagePerSecond, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            IDamageable damageable = GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = GetComponentInParent<IDamageable>();
            }

            if (damageable == null)
            {
                dotCoroutine = null;
                yield break;
            }

            // 초당 피해 -> tickInterval 기준 피해량으로 환산
            float tickDamage = damagePerSecond * tickInterval;
            damageable.OnDamage(tickDamage);

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        dotCoroutine = null;
    }
}