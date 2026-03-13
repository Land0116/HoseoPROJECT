using UnityEngine;

public class MonsterAttack_1_Basic : AttackPattern
{
    public GameObject bulletPrefab;
    public float fireRate = 1f;

    float timer;

    public override void Execute()
    {
        timer += Time.deltaTime;

        if( timer >= 1f/ fireRate)
        {
            Shoot();
            timer = 0f;
        }
    }

    void Shoot()
    {

        Vector3 dir = (monster.Player.position - monster.transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, monster.transform.position, Quaternion.identity);

        bullet.GetComponent<MonsterBullet>().SetDirection(dir);
    }
}
