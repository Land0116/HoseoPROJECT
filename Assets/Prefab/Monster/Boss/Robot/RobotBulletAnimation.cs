using UnityEngine;

public class RobotBulletAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Scale Multiplier")]
    [SerializeField] private float scaleMultiplier = 1f;

    private void Awake()
    {
        ApplyScale();
    }

    public void PlayMissileEffect()
    {
        if (animator == null) return;

        ApplyScale(); 
        animator.Play("Missile_Loop", 0, 0f);
    }

    private void ApplyScale()
    {
        // 항상 (1,1,1) 기준으로 배율 적용
        transform.localScale = Vector3.one * scaleMultiplier;
    }
}