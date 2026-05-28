using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerSpawner
/// 
/// 역할:
/// 1. 게임 플레이 씬에 들어왔을 때 Player가 없으면 처음 1번 생성
/// 2. 이미 Player가 있으면 새로 만들지 않고 현재 스포너 위치로 이동만 시킴
/// 3. Crosshair도 처음 1번만 생성하고 계속 재사용
/// 4. 씬이 바뀔 때마다 카메라 / UI / 참조 다시 연결
/// 
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("처음 생성할 프리팹")]
    [SerializeField] private PlayerController playerPrefab;
   

    private void Start()
    {
        // Main 씬이면 플레이어를 생성하거나 옮길 필요가 없음
        if (SceneManager.GetActiveScene().name == "Main")
            return;

        // Player가 아직 없는 경우 = 게임 시작 직후 or 처음 스테이지 진입
        if (PlayerController.Instance == null)
        {
            SpawnFirstPlayer();
        }
        // 이미 Player가 존재하는 경우 = 다음 스테이지로 이동한 상황
        else
        {
            RepositionExistingPlayer();
        }
    }

    /// <summary>
    /// 게임 시작 시 Player / Crosshair를 처음 1번 생성
    /// </summary>
    private void SpawnFirstPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("PlayerSpawner : playerPrefab이 비어 있음");
            return;
        }

        PlayerController spawnedPlayer = Instantiate(
            playerPrefab,
            transform.position,
            Quaternion.identity
        );

        DontDestroyOnLoad(spawnedPlayer.gameObject);

        // 이제 Crosshair Transform을 넘기지 않음
        spawnedPlayer.SetupAfterSpawn();

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

        CrosshairUI.Instance?.ShowCrosshair();
    }
    /// <summary>
    /// 이미 존재하는 Player를 현재 스테이지 스폰 위치로 이동
    /// 새 생성은 하지 않음
    /// </summary>
    private void RepositionExistingPlayer()
    {
        PlayerController existingPlayer = PlayerController.Instance;

        // 혹시 Instance가 없으면 종료
        if (existingPlayer == null) return;

        // 현재 스포너 위치로 Player 이동
        existingPlayer.transform.position = transform.position;

        // 이동 벡터 / 속도 / 공격 입력 정리
        existingPlayer.ResetVelocityOnly();

        // 새 씬의 카메라, 크로스헤어, 기타 참조 다시 연결
        existingPlayer.RefreshSceneReferences();

        // UI가 현재 Player를 다시 보도록 연결
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.BindPlayer(existingPlayer);
            PlayerUIManager.Instance.ShowPlayerHUD();
        }

        // 새 씬 카메라가 Player를 따라가게 다시 연결
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindCameraToPlayer(existingPlayer);
        }

        // 증강 슬롯 UI 다시 갱신
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.RefreshOwnedAugmentUI();
        }
        
        if (PlayerController.Instance != null &&
            !PlayerController.Instance.IsDie &&
            SceneManager.GetActiveScene().name != "Main")
        {
            CrosshairUI.Instance?.ShowCrosshair();
        }
    }
}