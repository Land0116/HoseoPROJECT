using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CurItemUI : MonoBehaviour
{
    public static CurItemUI Instance;

    [SerializeField] private Image itemImage;
    [Header("게임 시작 시 장착된 기본 아이템")]
    [SerializeField] private ItemData defaultItem;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //기본아이템
        if (defaultItem != null && defaultItem.icon != null)
        {
            itemImage.sprite = defaultItem.icon;
            itemImage.enabled = true;
        }
        else
        {
            itemImage.enabled = false;
        }
    }

    public void SetItem(ItemData itemData)
    {

        if (itemImage == null) return;

        if (itemData != null && itemData.icon != null)
        {
            itemImage.sprite = itemData.icon;
            itemImage.enabled = true;
        }
        else
        {
            itemImage.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


}