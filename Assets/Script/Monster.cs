using UnityEngine;
using UnityEngine.Rendering;

public class Monster : MonoBehaviour
{
    private float moveSpeed = 2f;
    private float moveRange = 2f;

    public GameObject monsterBullet;
    private float fireRate = 2f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private Transform player;
    private float fireTimer;

    private void Start()
    {
        startPos = transform.position;
        targetPos = GetRandomPosition();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        Move();
        Shoot();
    }

    void Move()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            targetPos = GetRandomPosition();
        }
    }
    Vector3 GetRandomPosition()
    {
        float randomX = Random.Range(-moveRange, moveRange);
        float randomY = Random.Range(-moveRange, moveRange);

        return startPos + new Vector3(randomX, randomY, 0);
    }

    void Shoot()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            fireTimer = 0;

            GameObject bullet = Instantiate(
                monsterBullet,
                transform.position,
                Quaternion.identity
            );

            Vector3 dir = (player.position - transform.position).normalized;

            bullet.GetComponent<MonsterBullet>().SetDirection(dir);
        }
    }
}
