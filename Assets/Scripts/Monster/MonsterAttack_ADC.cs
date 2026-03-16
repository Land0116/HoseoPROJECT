using UnityEngine;

public class MonsterAttack_ADC : AttackPattern
{
    public GameObject bulletPrefab;
    public float fireRate = 1f;
    public float attackRange = 5f;
    float timer;

    public override void Execute()
    {
        if (monster.Player == null) return;


        float distance = (monster.Player.position - monster.transform.position).sqrMagnitude;
        if (distance > attackRange * attackRange)
            return;

        timer += Time.deltaTime;

        if( timer >= 1f/ fireRate)
        {
            Shoot();
            timer = 0f;
        }
    }

    void Shoot()
    {
        if (monster.Player == null) return;

        Vector3 dir = (monster.Player.position - monster.transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity);

        bullet.GetComponent<MonsterBullet>().Init(dir);
    }
}
