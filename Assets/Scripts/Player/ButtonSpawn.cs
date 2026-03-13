using System;
using UnityEngine;

public class ButtonSpawn : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;

    public int damage = 50;
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
        TestEnemy testEnemy = other.GetComponent<TestEnemy>();
        if (other.gameObject.CompareTag("TestEnemy"))
        {
            testEnemy.OnDamage(damage);
            Destroy(gameObject);
        }
    }
}
