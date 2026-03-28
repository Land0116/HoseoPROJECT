// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class AugInventoryUI : MonoBehaviour
// {
//     public static AugInventoryUI instance;
//
//     [Header("증강 아이콘 슬롯")]
//     [SerializeField] private Image[] slotImages;
//
//     private void Awake()
//     {
//         instance = this;
//         Clear();
//     }
//
//     public void Refresh(List<AugmentationSystem> ownedAugments)
//     {
//         Clear();
//
//         for (int i = 0; i < ownedAugments.Count && i < slotImages.Length; i++)
//         {
//             if (ownedAugments[i] == null) continue;
//
//             slotImages[i].gameObject.SetActive(true);
//             slotImages[i].sprite = ownedAugments[i].icon;
//             slotImages[i].enabled = ownedAugments[i].icon != null;
//         }
//     }
//
//     private void Clear()
//     {
//         if (slotImages == null) return;
//
//         for (int i = 0; i < slotImages.Length; i++)
//         {
//             if (slotImages[i] == null) continue;
//
//             slotImages[i].sprite = null;
//             slotImages[i].enabled = false;
//             slotImages[i].gameObject.SetActive(false);
//         }
//     }
// }