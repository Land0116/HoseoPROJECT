using TMPro.EditorUtilities;
using UnityEngine;

public abstract class AttackPattern : MonoBehaviour
{
    protected Monster monster;

    [Header("?????? ??? ?????? ???? ????")]
    public bool blockMovement = false;

    public virtual void Init(Monster monster)
    {
        this.monster = monster;
    }

    public abstract void Execute();
}
