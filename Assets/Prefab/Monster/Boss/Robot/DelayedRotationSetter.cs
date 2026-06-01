using System.Collections;
using UnityEngine;

public class DelayedRotationSetter : MonoBehaviour
{
    [SerializeField] private float delay = 0.01f;
    [SerializeField] private Vector3 targetRotation;

    private void OnEnable()
    {
        StartCoroutine(ApplyRotation());
    }

    private IEnumerator ApplyRotation()
    {
        yield return new WaitForSeconds(delay);

        transform.rotation = Quaternion.Euler(targetRotation);
    }

    public void SetRotation(Vector3 rot)
    {
        targetRotation = rot;
    }

    public void SetRotation(float x, float y, float z)
    {
        targetRotation = new Vector3(x, y, z);
    }
}