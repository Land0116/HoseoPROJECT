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
}