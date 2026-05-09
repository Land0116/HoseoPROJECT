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

    private IEnumerator DotRoutine(float tickDamage, float duration)
    {
        IDamageable damageable = GetComponent<IDamageable>();

        if (damageable == null)
            damageable = GetComponentInParent<IDamageable>();

        if (damageable == null)
            yield break;

        float endTime = Time.time + duration;

        // 핵심:
        // ApplyDot 즉시 데미지를 주지 않고,
        // 1초 기다린 뒤 첫 틱 데미지를 준다.
        yield return new WaitForSeconds(1f);

        while (Time.time < endTime)
        {
            damageable.OnDamage(tickDamage);

            yield return new WaitForSeconds(1f);
        }

        dotCoroutine = null;
    }
}