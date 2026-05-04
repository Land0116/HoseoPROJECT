using UnityEngine;

public class MoveRandomly:MovePattern
{
    
    Vector3 targetPos;
    float timer;
    
    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsMovementLocked()) return;
        timer += Time.deltaTime;

        if (timer > 2f)
        {
            targetPos = monster.transform.position + new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(-3f, 3f),
                0
            );

            timer = 0;
        }

        Vector2 currentPos = monster.RB.position;

        Vector2 newPos = Vector2.MoveTowards(
            currentPos, targetPos, monster.GetMoveSpeed() * Time.deltaTime);
        monster.RB.MovePosition(newPos);
    }

}
