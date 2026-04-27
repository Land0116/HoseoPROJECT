using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [Header("처음 생성할 프리팹")]
    [SerializeField] private PlayerController playerPrefab;

    private void Start()
    {
        Debug.Log("[Spawner] Start 실행됨");

        Debug.Log($"[Spawner] CrosshairSingleton null? {CrosshairSingleton.Instance == null}");

        if (SceneManager.GetActiveScene().name == "Main")
            return;

        if (PlayerController.Instance == null)
        {
            SpawnFirstPlayer();
        }
        else
        {
            RepositionExistingPlayer();
        }
    }

    private void SpawnFirstPlayer()
    {
        if (playerPrefab == null) return;

        PlayerController spawnedPlayer = Instantiate(
            playerPrefab,
            transform.position,
            Quaternion.identity
        );

        DontDestroyOnLoad(spawnedPlayer.gameObject);

        Transform crosshair = CrosshairSingleton.Instance != null
            ? CrosshairSingleton.Instance.Crosshair
            : null;

        spawnedPlayer.SetupAfterSpawn(crosshair);

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.BindPlayer(spawnedPlayer);
            PlayerUIManager.Instance.ShowPlayerHUD();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindCameraToPlayer(spawnedPlayer);
        }

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }

    private void RepositionExistingPlayer()
    {
        PlayerController existingPlayer = PlayerController.Instance;
        if (existingPlayer == null) return;

        existingPlayer.transform.position = transform.position;
        existingPlayer.ResetVelocityOnly();
        existingPlayer.RefreshSceneReferences();

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.BindPlayer(existingPlayer);
            PlayerUIManager.Instance.ShowPlayerHUD();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindCameraToPlayer(existingPlayer);
        }

        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
    }
}