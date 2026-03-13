using TMPro.EditorUtilities;
using UnityEngine;

public abstract class AttackPattern : MonoBehaviour
{
    protected Monster monster;

    public virtual void Init(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void Execute();
}
