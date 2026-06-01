using UnityEngine;

public class RobotBulletAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void PlayMissileEffect()
    {
        if (animator == null) return;

        animator.Play("Missile_Loop", 0, 0f);
    }
}