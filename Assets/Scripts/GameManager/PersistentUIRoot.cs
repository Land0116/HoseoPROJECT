using UnityEngine;

public class PersistentUIRoot : MonoBehaviour
{
    public static PersistentUIRoot Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}