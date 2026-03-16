using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AugUIManager : MonoBehaviour
{
    public static AugUIManager instance;

    [Header("전체 증강 데이터")]
    public AugmentationSystem[] augmentationDatabase;

    [Header("UI 버튼 3개")]
    public AugButton[] uiButtons;

    [Header("증강 선택 패널")]
    public GameObject uiPanel;

    [Header("증강 선택 제한")]
    [SerializeField] private int maxSelectCount = 5;

    private List<AugmentationSystem> ownedAugments = new List<AugmentationSystem>();

    public bool IsSelecting { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public bool CanShowAugmentation()
    {
        if (augmentationDatabase == null || augmentationDatabase.Length == 0) return false;
        if (ownedAugments.Count >= maxSelectCount) return false;

        int remainCount = augmentationDatabase.Count(aug => aug != null && aug.isUnlocked && !ownedAugments.Contains(aug));
        return remainCount > 0;
    }

    public void ShowAugmentation()
    {
        if (!CanShowAugmentation())
        {
            uiPanel.SetActive(false);
            return;
        }

        List<AugmentationSystem> selectedAugments = GetRandomAugments(3);

        if (selectedAugments.Count == 0)
        {
            uiPanel.SetActive(false);
            return;
        }

        IsSelecting = true;
        Time.timeScale = 0f;
        uiPanel.SetActive(true);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControl(false);
        }

        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (i < selectedAugments.Count)
            {
                uiButtons[i].gameObject.SetActive(true);
                uiButtons[i].Setup(selectedAugments[i], this);
            }
            else
            {
                uiButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private List<AugmentationSystem> GetRandomAugments(int count)
    {
        List<AugmentationSystem> result = new List<AugmentationSystem>();

        List<AugmentationSystem> availableList = augmentationDatabase
            .Where(aug => aug != null && aug.isUnlocked && !ownedAugments.Contains(aug))
            .ToList();

        while (result.Count < count && availableList.Count > 0)
        {
            AugmentationSystem.AugmentationType targetType = GetWeightedType();

            List<AugmentationSystem> typeCandidates = availableList
                .Where(aug => aug.augmentationType == targetType)
                .ToList();

            List<AugmentationSystem> candidates = typeCandidates.Count > 0
                ? typeCandidates
                : availableList;

            int rand = Random.Range(0, candidates.Count);
            AugmentationSystem picked = candidates[rand];

            result.Add(picked);
            availableList.Remove(picked);
        }

        return result;
    }

    private AugmentationSystem.AugmentationType GetWeightedType()
    {
        float rand = Random.Range(0f, 100f);

        if (rand < 40f)
            return AugmentationSystem.AugmentationType.Atk;
        if (rand < 70f)
            return AugmentationSystem.AugmentationType.Dfs;

        return AugmentationSystem.AugmentationType.Util;
    }

    public void SelectAugmentation(AugmentationSystem selectedData)
    {
        if (selectedData == null) return;
        if (ownedAugments.Count >= maxSelectCount) return;

        if (!ownedAugments.Contains(selectedData))
        {
            ownedAugments.Add(selectedData);
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ApplyAugmentation(selectedData);
        }

        if (AugInventoryUI.instance != null)
        {
            AugInventoryUI.instance.Refresh(ownedAugments);
        }

        CloseAugmentation();
    }

    public void CloseAugmentation()
    {
        IsSelecting = false;
        uiPanel.SetActive(false);
        Time.timeScale = 1f;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControl(true);
        }
    }

    public int GetOwnedCount()
    {
        return ownedAugments.Count;
    }

    public List<AugmentationSystem> GetOwnedAugments()
    {
        return ownedAugments;
    }
}