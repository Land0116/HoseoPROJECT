using System;
using UnityEngine;

[Serializable]
public class AugmentSlotData
{
    [Header("슬롯 상태")]
    public bool isOccupied = false;

    [Header("현재 슬롯에 들어있는 증강")]
    public AugmentationSystem augmentData;

    [Header("현재 레벨 / 중첩")]
    [Min(1)] public int currentLevel = 1;
    [Min(1)] public int stackCount = 1;

    #region Utility

    public void Clear()
    {
        isOccupied = false;
        augmentData = null;
        currentLevel = 1;
        stackCount = 1;
    }

    public void Set(AugmentationSystem newAugment)
    {
        isOccupied = newAugment != null;
        augmentData = newAugment;
        currentLevel = 1;
        stackCount = 1;
    }

    #endregion
}