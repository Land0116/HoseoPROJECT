using System;
using UnityEngine;

public class ButtonSpawn : MonoBehaviour
{
    public float lifeTime = 1f;

    private float speed = 5.0f;
    private float damage;
    public void SetDamage(float value)
    {
        damage = value;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PlayerController.Instance != null && other.gameObject == PlayerController.Instance.gameObject)
            return;

        if (!other.CompareTag("TestEnemy"))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        damageable.OnDamage(damage);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHitEnemy(damage);
        }

        Destroy(gameObject);
    }
}
