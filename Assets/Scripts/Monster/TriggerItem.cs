using UnityEngine;

public class TriggerItem : MonoBehaviour
{
    [SerializeField] private MonsterSpawner spawner;

    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        MonsterSpawner[] spawners = FindObjectsByType<MonsterSpawner>(FindObjectsSortMode.None);

        foreach (var spawner in spawners)
        {
            spawner.SetSpawn(true);
        }

        Destroy(gameObject);
    }
}