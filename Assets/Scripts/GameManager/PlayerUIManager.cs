using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public PlayerController playerController;
    public AugmentationSystem[] augmentationSystem;
    public static PlayerUIManager Instance { get; private set; }
    
    [Header("PlayerTextUI")]
    [SerializeField] private GameObject playerTextUIPanel;
    [SerializeField] private TextMeshProUGUI Amount;
    [SerializeField] private TextMeshProUGUI reloadText;
    
    
    [Header("AugmentationUI")]
    [SerializeField] private GameObject augmentationUIPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTextUIPanel = GameObject.Find("PlayerUIPanel").transform.Find("ReloadingTxt").gameObject;
        // if (playerData == null)
        // {
        //     // Resources 폴더에서 해당 이름의 에셋을 찾아 할당합니다.
        //     playerData = Resources.Load<PlayerData>("player/playerBaseData");
        // }
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환시 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Amount.text = playerController.Amount.ToString() + "/" + playerController.MaxAmount.ToString();
    }
    
    public IEnumerator ReloadingText()
    {
        reloadText.gameObject.SetActive(true);
        reloadText.text = "Reloading.";
        yield return new WaitForSeconds(0.5f);
        reloadText.text = "Reloading..";
        yield return new WaitForSeconds(0.5f);
        reloadText.text = "Reloading...";
        yield return new WaitForSeconds(0.5f);
        reloadText.gameObject.SetActive(false);
    }
}
