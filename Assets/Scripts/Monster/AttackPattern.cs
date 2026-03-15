using TMPro.EditorUtilities;
using UnityEngine;

public abstract class AttackPattern : MonoBehaviour
{
    protected Monster monster;

    [Header("공격을 할때 움직임 가능 여부")]
    public bool blockMovement = false;

    public virtual void Init(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void Execute();
}
