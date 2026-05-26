using UnityEngine;
using System.Collections;

public class FallAttackDamage : MonoBehaviour
{
    private float damage;

    public void Init(float dmg)
    {
        damage = dmg;
        StartCoroutine(Life());
    }

    private IEnumerator Life()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Monster")) return;

        Monster m = other.GetComponent<Monster>();
        if (m != null)
        {
            m.OnDamage(damage);
        }
    }
}