using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

[CreateAssetMenu(menuName = "ItemEffect/Hourglass")]
public class HourglassEffect : ItemEffect
{
    public float slowMultiplier = 0.7f;
    public float duration = 2f;

    public override void Use(GameObject user)
    {
        CoroutineRunner.Instance.StartCoroutine(SlowTime());
    }

    private IEnumerator SlowTime()
    {
        Time.timeScale = slowMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}