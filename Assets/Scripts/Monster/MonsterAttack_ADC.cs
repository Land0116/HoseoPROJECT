using UnityEngine;

public class MonsterAttack_ADC : AttackPattern
{
    public GameObject bulletPrefab;
    public float fireRate = 1f;
    public float attackRange = 5f;

    private float timer;
    private bool wasInRange = false;

    public override void Execute()
    {
        if (monster == null || monster.Player == null) return;

        float distance = Vector2.Distance(monster.Player.position, monster.transform.position);
        bool isInRange = distance <= attackRange;


        if (isInRange && !wasInRange)
        {
            monster.ShowAttackAlert(0.3f);
        }

        if (!isInRange)
        {
            timer = 0f;
            wasInRange = false;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= 1f / fireRate)
        {
            Shoot();
            timer = 0f;
        }

        wasInRange = isInRange;
    }

    void Shoot()
    {
        Vector3 dir = (monster.Player.position - monster.transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            monster.transform.position,
            Quaternion.identity
        );

        bullet.GetComponent<MonsterBullet>().SetDirection(dir);
    }
}