using UnityEngine;

namespace Player.State
{
    public class PlayerAttackAnimationEndBehaviour : StateMachineBehaviour
    {
        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            PlayerController player = animator.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            player.NotifyAttackAnimationFinished();
        }
    }
}