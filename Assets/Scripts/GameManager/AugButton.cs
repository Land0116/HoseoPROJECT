using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugButton : MonoBehaviour
{
    #region Fields

    [Header("버튼 내부 UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    [Header("선택 버튼")]
    [SerializeField] private Button selectBtn;

    private int currentDisplayLevel = 1;
    private AugmentationSystem currentData;
    private AugUIManager currentManager;
    private bool isClicked = false;

    #endregion

    #region Public

    public void Setup(AugmentationSystem data, AugUIManager manager, int displayLevel)
    {
        currentData = data;
        currentManager = manager;
        currentDisplayLevel = Mathf.Max(1, displayLevel);
        isClicked = false;

        if (nameText != null)
            nameText.text = data != null ? data.augmentationName : "";

        if (descText != null)
            descText.text = GetFormattedDescription(data, displayLevel);

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.enabled = data != null && data.icon != null;
        }

        if (selectBtn != null)
        {
            selectBtn.onClick.RemoveAllListeners();
            selectBtn.onClick.AddListener(OnClickSelect);
            selectBtn.interactable = data != null && manager != null;
        }
    }

    #endregion

    #region Description Format

    /// <summary>
    /// 증강 설명 문자열에 들어있는 {0}, {1} 자리에
    /// 실제 레벨 1 기준 수치를 넣어서 반환한다.
    /// 
    /// 예:
    /// "추가 투사체 {0}개 발사, 데미지 {1}배"
    /// -> "추가 투사체 2개 발사, 데미지 0.8배"
    /// </summary>
    private string GetFormattedDescription(AugmentationSystem data, int level)
    {
        if (data == null)
            return "";

        // 설명문 자체가 비어 있으면 빈 문자열 반환
        if (string.IsNullOrEmpty(data.augmentationDesc))
            return "";

        switch (data.category)
        {
            case AugmentationSystem.AugmentCategory.SubSkill:
                return GetFormattedSubSkillDescription(data, level);

            case AugmentationSystem.AugmentCategory.Passive:
                return GetFormattedPassiveDescription(data, level);

            case AugmentationSystem.AugmentCategory.Special:
                return GetFormattedSpecialDescription(data, level);
        }

        // 혹시 분류가 이상하면 원문 그대로 반환
        return data.augmentationDesc;
    }

    /// <summary>
    /// 서브 스킬형 설명 포맷
    /// </summary>
    private string GetFormattedSubSkillDescription(AugmentationSystem data, int level)
    {
        AugmentationSystem.SubSkillLevelData levelData = data.GetSubSkillLevelData(level);

        if (levelData == null)
            return data.augmentationDesc;

        try
        {
            switch (data.subSkillType)
            {
                case AugmentationSystem.SubSkillType.Shot:
                    return string.Format(
                        data.augmentationDesc,
                        levelData.projectileCountAdd,
                        levelData.damageMultiplier,
                        levelData.spreadAngle,
                        levelData.chargeTime
                    );

                case AugmentationSystem.SubSkillType.Trajectory:
                    return string.Format(
                        data.augmentationDesc,
                        levelData.homingStrength,
                        levelData.trajectoryDuration,
                        levelData.bounceCount,
                        levelData.pierceCount
                    );

                case AugmentationSystem.SubSkillType.Effect:
                    return string.Format(
                        data.augmentationDesc,
                        levelData.explosionRadius,
                        levelData.explosionDamageMultiplier,
                        levelData.chainCount,
                        levelData.chainRange,
                        levelData.dotDamagePerSecond,
                        levelData.dotDuration
                    );
            }
        }
        catch
        {
            return data.augmentationDesc;
        }

        return data.augmentationDesc;
    }
    
    /// <summary>
    /// 패시브형 설명 포맷
    /// </summary>
    private string GetFormattedPassiveDescription(AugmentationSystem data, int level)
    {
        AugmentationSystem.PassiveLevelData levelData = data.GetPassiveLevelData(level);

        if (levelData == null)
            return data.augmentationDesc;

        try
        {
            switch (data.passiveType)
            {
                case AugmentationSystem.PassiveType.Damage:
                    return string.Format(data.augmentationDesc, levelData.damageMultiplier);

                case AugmentationSystem.PassiveType.AttackSpeed:
                    return string.Format(data.augmentationDesc, levelData.attackSpeedPercent * 100f);

                case AugmentationSystem.PassiveType.MaxHp:
                    return string.Format(data.augmentationDesc, levelData.maxHpAdd);

                case AugmentationSystem.PassiveType.LifeSteal:
                    return string.Format(data.augmentationDesc, levelData.lifeStealPercent * 100f);
            }
        }
        catch
        {
            return data.augmentationDesc;
        }

        return data.augmentationDesc;
    }

    /// <summary>
    /// 특수형 설명 포맷
    /// 현재 특수형은 상세 수치 설계가 덜 되어 있으므로
    /// level 1 설명문을 그대로 사용하거나 ruleDescription을 우선 사용한다.
    /// </summary>
    private string GetFormattedSpecialDescription(AugmentationSystem data, int level)
    {
        AugmentationSystem.SpecialLevelData levelData = data.GetSpecialLevelData(level);

        if (levelData == null)
            return data.augmentationDesc;

        if (!string.IsNullOrEmpty(levelData.ruleDescription))
            return levelData.ruleDescription;

        return data.augmentationDesc;
    }
    
    #endregion

    #region Button Event

    private void OnClickSelect()
    {
        if (isClicked) return;
        if (currentManager == null) return;
        if (currentData == null) return;

        isClicked = true;

        if (selectBtn != null)
            selectBtn.interactable = false;

        currentManager.SelectAugmentation(currentData);
    }

    #endregion
}