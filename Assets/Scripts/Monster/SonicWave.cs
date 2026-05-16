using System.Collections.Generic;
using UnityEngine;

public class SonicWave : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.5f;
    [SerializeField] private float damage = 10f;

    public System.Action onDestroy;

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnDestroy()
    {
        onDestroy?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        IDamageable target = collision.GetComponent<IDamageable>();
        if (target == null) return;

        if (hitTargets.Contains(target)) return;

        hitTargets.Add(target);
        target.OnDamage(damage);
    }
}