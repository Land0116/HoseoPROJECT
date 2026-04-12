using UnityEngine;

public class SceneMoveTriggerRelay : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (StageClear.Instance != null)
        {
            StageClear.Instance.TryMoveNextScene(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (StageClear.Instance != null)
        {
            StageClear.Instance.TryMoveNextScene(other);
        }
    }
}