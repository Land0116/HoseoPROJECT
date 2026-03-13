using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PlayerData playerData;
    
    [Header("PlayerTextUI")]
    [SerializeField] private GameObject playerTextUICanvas;
    [SerializeField] private TextMeshProUGUI Amount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerData == null)
        {
            // Resources 폴더에서 해당 이름의 에셋을 찾아 할당합니다.
            playerData = Resources.Load<PlayerData>("player/playerBaseData");
        }
    }

    // Update is called once per frame
    void Update()
    {
        Amount.text = playerData.Amount.ToString() + "/" + playerData.MaxAmount.ToString();
    }
}
