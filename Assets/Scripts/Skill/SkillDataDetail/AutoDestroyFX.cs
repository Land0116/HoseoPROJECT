using UnityEngine;

public class AutoDestroyFX : MonoBehaviour
{
    public float lifeTime = 0.2f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}