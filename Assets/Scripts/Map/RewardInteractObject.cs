using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전투방 클리어 후 생성되는 보상 상호작용 오브젝트.
/// - BodyCollider: 플레이어가 오브젝트 위로 올라가지 못하게 막는 물리 충돌용
/// - InteractCollider: F키 상호작용 감지용 Trigger
/// </summary>
public class RewardInteractObject : AutoBindableBehaviour
{
    [Header("보상 타입")]
    [SerializeField] private RewardType rewardType = RewardType.None;

    [Header("아이콘 렌더러")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("몸통 충돌 콜라이더 - IsTrigger false")]
    [SerializeField] private Collider2D bodyCollider;

    [Header("상호작용 감지 콜라이더 - IsTrigger true")]
    [SerializeField] private Collider2D interactCollider;

    private Action onRewardFinished;

    private bool isPlayerNear;
    private bool isUsed;

    protected override void AutoBindCore()
    {
        AutoBindIconRenderer();
        AutoBindColliders();
    }

    private void AutoBindIconRenderer()
    {
        if (iconRenderer != null) return;

        iconRenderer = AutoBindUtility.FindComponentInChildByName<SpriteRenderer>(
            transform,
            "IconRenderer"
        );

        if (iconRenderer != null) return;

        iconRenderer = AutoBindUtility.FindComponentInChildByName<SpriteRenderer>(
            transform,
            "RewardIcon"
        );

        if (iconRenderer != null) return;

        iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void AutoBindColliders()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null) continue;

            if (col.isTrigger)
            {
                if (interactCollider == null)
                    interactCollider = col;
            }
            else
            {
                if (bodyCollider == null)
                    bodyCollider = col;
            }
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }

        if (interactCollider != null)
        {
            interactCollider.isTrigger = true;
        }

        if (bodyCollider == null)
        {
            Debug.LogWarning("[RewardInteractObject] BodyCollider가 없음. 플레이어가 보상 오브젝트를 통과할 수 있음.");
        }

        if (interactCollider == null)
        {
            Debug.LogWarning("[RewardInteractObject] InteractCollider가 없음. F키 상호작용 감지가 안 될 수 있음.");
        }

        if (bodyCollider != null && interactCollider != null && bodyCollider == interactCollider)
        {
            Debug.LogWarning("[RewardInteractObject] BodyCollider와 InteractCollider가 같은 콜라이더임. 콜라이더를 2개로 분리해야 함.");
        }
    }

    public void Setup(RewardType newRewardType, Sprite icon, Action rewardFinishedCallback)
    {
        AutoBind();

        rewardType = newRewardType;
        onRewardFinished = rewardFinishedCallback;

        isPlayerNear = false;
        isUsed = false;

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (interactCollider != null)
            interactCollider.enabled = true;

        ApplyIcon(icon);
    }

    private void ApplyIcon(Sprite icon)
    {
        if (iconRenderer == null) return;

        iconRenderer.sprite = icon;
        iconRenderer.enabled = icon != null;
    }

    private void Update()
    {
        if (isUsed) return;
        if (!isPlayerNear) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    private void Interact()
    {
        if (isUsed) return;

        Debug.Log($"[RewardInteractObject] 상호작용 보상 타입: {rewardType}");

        isUsed = true;
        isPlayerNear = false;

        PlayerUIManager.Instance?.HideInteractObjectPanel();

        if (interactCollider != null)
            interactCollider.enabled = false;

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        if (iconRenderer != null)
            iconRenderer.enabled = false;

        switch (rewardType)
        {
            case RewardType.Augment:
                OpenAugmentReward();
                break;

            case RewardType.Skill:
                OpenSkillReward();
                break;

            case RewardType.Item:
                SpawnItemReward();
                break;

            default:
                FinishReward();
                break;
        }
    }

    private void OpenAugmentReward()
    {
        if (AugUIManager.instance != null)
        {
            AugUIManager.instance.OpenAugmentPanelFromReward(FinishReward);
        }
        else
        {
            Debug.LogWarning("[RewardInteractObject] AugUIManager가 없음. 증강 보상 완료 처리.");
            FinishReward();
        }
    }

    private void OpenSkillReward()
    {
        if (SkillSelectUIManager.Instance != null)
        {
            SkillSelectUIManager.Instance.OpenSkillPanelFromReward(FinishReward);
        }
        else
        {
            Debug.LogWarning("[RewardInteractObject] SkillSelectUIManager가 없음. 스킬 보상 완료 처리.");
            FinishReward();
        }
    }

    private void SpawnItemReward()
    {
        if (NewItemUIManager.Instance != null)
        {
            NewItemUIManager.Instance.SpawnItemRewardFromReward(FinishReward);
        }
        else
        {
            Debug.LogWarning("[RewardInteractObject] NewItemUIManager가 없음. 아이템 보상 완료 처리.");
            FinishReward();
        }
    }

    private void FinishReward()
    {
        onRewardFinished?.Invoke();
        onRewardFinished = null;

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (isUsed) return;
        
        isPlayerNear = true;
        PlayerUIManager.Instance?.ShowInteractObjectPanel(GetPromptText());
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerNear = false;
        PlayerUIManager.Instance?.HideInteractObjectPanel();


    }
    
    private string GetPromptText()
    {
        switch (rewardType)
        {
            case RewardType.Augment:
                return "F키로 증강 선택";

            case RewardType.Skill:
                return "F키로 스킬 선택";

            case RewardType.Item:
                return "F키로 아이템 획득";

            default:
                return "F키로 상호작용";
        }
    }
}