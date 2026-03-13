using System;
using UnityEngine;

public class TestEnemy : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float health;

    private float Damage;

    void Awake()
    {
        health = maxHealth;
        Damage = 5;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("플레이어와 접촉");
            other.gameObject.GetComponent<IDamageable>().OnDamage(Damage);
        }
        else if (other.gameObject.CompareTag("CrossHair"))
        {
            Debug.Log("조준점과 접촉");
        }
        
    }
    //데미지 입음
    public void OnDamage(float damage)
    {
        health -= damage;
        Debug.Log("[" + health + "]" + "남음");

        if (health <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        Destroy(this.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("플레이어 나감");
        }
        else if (other.gameObject.CompareTag("CrossHair"))
        {
            Debug.Log("조준점 나감");
        }
    }
}
