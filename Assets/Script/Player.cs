using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    private int moveRange = 1;

    float startX;
    private int moveDirection = 1;

    Camera cam;

    private void Start()
    {
        startX = transform.position.x;
        cam = Camera.main;

    }
    private void Update()
    {
        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        transform.Translate(camRight * moveDirection * moveSpeed * Time.deltaTime, Space.World);

        if(transform.position.x > startX + moveRange)
        {
            moveDirection = -1;
        }
        else if(transform.position.x < startX - moveRange)
        {
            moveDirection = 1;
        }
    }
}
