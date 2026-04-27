using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CurItemUI : MonoBehaviour
{
    public static CurItemUI Instance;

    [SerializeField] private Image itemImage;
    [Header("게임 시작 시 장착된 기본 아이템")]
    [SerializeField] private ItemData defaultItem;
    [SerializeField] private Image[] itemSlots;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 전부 비활성화
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].enabled = false;
        }
    }

    public void SetItems(List<ItemData> items)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < items.Count && items[i] != null && items[i].icon != null)
            {
                itemSlots[i].sprite = items[i].icon;
                itemSlots[i].enabled = true;
            }
            else
            {
                itemSlots[i].enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


}