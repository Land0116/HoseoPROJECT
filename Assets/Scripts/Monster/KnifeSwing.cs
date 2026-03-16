using UnityEngine;

public class KnifeSwing : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float damage = 10f;
    private float rotated;

    private void Update()
    {
        float step = rotateSpeed * Time.deltaTime;

        transform.Rotate(0, 0, step);
        rotated += step;

        if(rotated >= swingAngle)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        IDamageable damageable = collision.GetComponent<IDamageable>();

        if(damageable != null)
        {
            damageable.OnDamage(damage);
        }
    }
}
