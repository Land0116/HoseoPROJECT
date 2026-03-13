using UnityEngine;

public abstract class MovePattern : MonoBehaviour
{
    protected Monster monster;
    public virtual void Init(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void Execute();
}
