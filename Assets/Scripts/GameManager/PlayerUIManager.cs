using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public PlayerController playerController;
    public static PlayerUIManager Instance { get; private set; }
    
    [Header("PlayerTextUI")]
    [SerializeField] private GameObject playerTextUIPanel;

    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI reloadText;
    
    
    [Header("AugmentationUI")]
    [SerializeField] private GameObject augmentationUIPanel;

    void Start()
    {
        playerTextUIPanel = GameObject.Find("PlayerUIPanel").transform.Find("ReloadingTxt").gameObject;
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        PlayerStateUI();
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

    private void PlayerStateUI()
    {
        hpText.text = playerController.Hp.ToString() + " / " + playerController.MaxHp.ToString();
        hpBar.value = Mathf.Lerp(hpBar.value, (float)playerController.Hp / (float)playerController.MaxHp, Time.deltaTime);
        if(playerController.IsDie) return;
        amountText.text = playerController.Amount.ToString() + "/" + playerController.MaxAmount.ToString();
        goldText.text = "G : " + playerController.Gold.ToString();
        
        
    }
}
