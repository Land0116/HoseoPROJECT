using UnityEngine;

public class MoveRandomly:MovePattern
{
    
    Vector3 targetPos;
    float timer;

    public override void Execute()
    {
        if (monster == null) return;

        timer += Time.deltaTime;

        if (timer > 2f)
        {
            targetPos = monster.transform.position + new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                0
            );

            timer = 0;
        }

        monster.transform.position = Vector3.MoveTowards(
            monster.transform.position,
            targetPos,
            monster.MoveSpeed * Time.deltaTime
        );
    }
}
