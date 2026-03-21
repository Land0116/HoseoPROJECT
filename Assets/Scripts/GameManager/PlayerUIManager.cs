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

    void Start()
    {
        playerTextUIPanel = GameObject.Find("PlayerUIPanel").transform.Find("ReloadingTxt").gameObject;

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
