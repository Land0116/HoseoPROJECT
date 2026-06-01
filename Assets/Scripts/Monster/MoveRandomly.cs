using UnityEngine;

public class MoveRandomly : MovePattern
{
    [SerializeField] private bool loopMove = false;

    Vector3 targetPos;
    float timer;

    public override void Execute()
    {
        if (monster == null) return;
        if (monster.IsMovementLocked()) return;

        Vector2 currentPos = monster.RB.position;

        timer += Time.deltaTime;

        float distanceToTarget = Vector2.Distance(currentPos, targetPos);

        if (loopMove)
        {
            if (timer >= 1f || targetPos == Vector3.zero || distanceToTarget < 0.2f)
            {
                SetNewTarget2();
                timer = 0f;
            }
        }
        else
        {
            if (timer >= 2f || targetPos == Vector3.zero || distanceToTarget < 0.2f)
            {
                SetNewTarget();
                timer = 0f;
            }
        }

        Vector2 newPos = Vector2.MoveTowards(
            currentPos,
            targetPos,
            monster.GetMoveSpeed() * Time.deltaTime
        );

        monster.RB.MovePosition(newPos);
    }

    private void SetNewTarget()
    {
        targetPos = monster.transform.position + new Vector3(
            Random.Range(-3f, 3f),
            Random.Range(-3f, 3f),
            0
        );
    }

    private void SetNewTarget2()
    {
        targetPos = monster.transform.position + new Vector3(
            Random.Range(-3f, 3f),
            Random.Range(-3f, 3f),
            0
        );
    }
}