using UnityEngine;

public class targetCamera : MonoBehaviour
{
    public Transform player;

    [SerializeField]
    private int add_Y = 10;
    private int add_X = -10;

    private void LateUpdate()
    {
        transform.position = new Vector3(
            player.position.x,
            player.position.y + add_Y,
            player.position.z + add_X);
    }
}
