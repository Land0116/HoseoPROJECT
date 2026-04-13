using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    public void FireOnAnimationEvent()
    {
        if (PlayerController.Instance != null)
        {
           PlayerController.Instance.FireOnAnimationEvent();
        }
    }

    public void EndAttackAnimationEvent()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EndAttackAnimationEvent();
        }
    }
    public void EndHitAnimationEvent()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EndHitAnimationEvent();
        }
    }
    
    public void EndDeathAnimationEvent()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EndDeathAnimationEvent();
        }
    }
}