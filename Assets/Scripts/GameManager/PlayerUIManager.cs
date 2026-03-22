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
    [SerializeField] private TextMeshProUGUI goldText;
    
    
    [Header("AugmentationUI")]
    [SerializeField] private GameObject augmentationUIPanel;

    void Start()
    {
        
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
    

    private void PlayerStateUI()
    {
        if (playerController.Hp <= 0)
        {
            playerController.Hp = 0;
        }
        hpText.text = playerController.Hp.ToString() + " / " + playerController.MaxHp.ToString();
        hpBar.value = Mathf.Lerp(hpBar.value, (float)playerController.Hp / (float)playerController.MaxHp, Time.deltaTime);
        if(playerController.IsDie) return;
        goldText.text = "G : " + playerController.Gold.ToString();
        
    }
}
