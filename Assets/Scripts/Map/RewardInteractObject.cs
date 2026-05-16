using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전투방 클리어 후 생성되는 보상 상호작용 오브젝트.
/// 
/// 역할:
/// - RewardObjectSpawner가 플레이어 근처에 생성함
/// - RewardType에 따라 아이콘 Sprite를 표시함
/// - 플레이어가 근처에서 F키를 누르면 보상 처리함
/// - 보상 처리가 끝나면 CombatRoomController 쪽으로 완료 콜백을 보냄
/// 
/// AutoBind 구조:
/// - Awake / OnValidate / Reset / ContextMenu Auto Bind는 AutoBindableBehaviour가 담당
/// - 이 스크립트는 AutoBindCore()만 override해서 필요한 참조만 연결
/// </summary>
public class RewardInteractObject : AutoBindableBehaviour
{
    [Header("보상 타입")]
    [SerializeField] private RewardType rewardType = RewardType.None;

    [Header("아이콘 렌더러")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("상호작용 콜라이더")]
    [SerializeField] private Collider2D interactCollider;

    /// <summary>
    /// 보상 처리가 완전히 끝났을 때 호출할 콜백.
    /// 
    /// CombatRoomController 쪽에서는 이 콜백이 호출되어야
    /// 다음 출구를 배정하고 문을 열 수 있다.
    /// </summary>
    private Action onRewardFinished;

    /// <summary>
    /// 플레이어가 보상 오브젝트 상호작용 범위 안에 있는지 여부.
    /// </summary>
    private bool isPlayerNear;

    /// <summary>
    /// 이미 한 번 사용된 보상인지 여부.
    /// 
    /// F키 연타로 중복 호출되는 것을 막기 위한 플래그다.
    /// </summary>
    private bool isUsed;

    /// <summary>
    /// AutoBindableBehaviour에서 호출되는 실제 자동 바인딩 함수.
    /// 
    /// 기존 Awake / OnValidate / Reset에서 호출하던
    /// private AutoBind() 내용을 여기로 옮긴 것이다.
    /// </summary>
    protected override void AutoBindCore()
    {
        AutoBindIconRenderer();

        AutoBindInteractCollider();
    }

    /// <summary>
    /// 아이콘 표시용 SpriteRenderer를 자동으로 찾는다.
    /// 
    /// 우선순위:
    /// 1. 이름이 IconRenderer인 자식
    /// 2. 이름이 RewardIcon인 자식
    /// 3. 현재 오브젝트 또는 자식의 첫 번째 SpriteRenderer
    /// 
    /// 프리팹 구조가 바뀌어도 최대한 자동으로 잡히게 하기 위한 처리다.
    /// </summary>
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

    /// <summary>
    /// 플레이어 감지용 Collider2D를 자동으로 찾는다.
    /// 
    /// RewardInteractObject는 플레이어와 물리 충돌하는 오브젝트가 아니라
    /// 플레이어가 근처에 있는지 감지하는 오브젝트이므로
    /// Collider2D는 Trigger로 사용하는 것이 맞다.
    /// </summary>
    private void AutoBindInteractCollider()
    {
        if (interactCollider == null)
        {
            interactCollider = GetComponent<Collider2D>();
        }

        if (interactCollider != null)
        {
            interactCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// RewardObjectSpawner가 보상 오브젝트를 생성한 직후 호출하는 초기화 함수.
    /// 
    /// newRewardType:
    /// - Augment
    /// - Skill
    /// - Item
    /// 
    /// icon:
    /// - 보상 타입에 맞는 Sprite
    /// 
    /// rewardFinishedCallback:
    /// - 보상 처리가 끝났을 때 CombatRoomController에 알려줄 콜백
    /// </summary>
    public void Setup(RewardType newRewardType, Sprite icon, Action rewardFinishedCallback)
    {
        // 혹시 프리팹 생성 직후 참조가 비어있는 경우를 대비해서 한 번 더 보장한다.
        AutoBind();

        rewardType = newRewardType;
        onRewardFinished = rewardFinishedCallback;

        isPlayerNear = false;
        isUsed = false;

        if (interactCollider != null)
        {
            interactCollider.enabled = true;
        }

        ApplyIcon(icon);
    }

    /// <summary>
    /// 현재 보상 타입에 맞는 아이콘을 적용한다.
    /// 
    /// icon이 null이면 SpriteRenderer를 숨긴다.
    /// </summary>
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

    /// <summary>
    /// 플레이어가 F키를 눌렀을 때 실행되는 보상 상호작용 함수.
    /// 
    /// 한 번 사용되면:
    /// - isUsed = true
    /// - Collider 비활성화
    /// - 아이콘 숨김
    /// 
    /// 이후 rewardType에 따라 보상 처리를 분기한다.
    /// </summary>
    private void Interact()
    {
        if (isUsed) return;

        Debug.Log($"[RewardInteractObject] 상호작용 보상 타입: {rewardType}");
        isUsed = true;

        // 상호작용 후에는 다시 누르지 못하게 콜라이더를 끈다.
        if (interactCollider != null)
        {
            interactCollider.enabled = false;
        }

        // 플레이어 눈에는 바로 사라진 것처럼 보이게 아이콘을 숨긴다.
        if (iconRenderer != null)
        {
            iconRenderer.enabled = false;
        }

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

    /// <summary>
    /// 증강 보상 처리.
    /// 
    /// 증강 보상은 바로 완료하지 않고,
    /// AugUIManager의 증강 선택 패널을 연다.
    /// 
    /// 플레이어가 증강을 선택하면
    /// AugUIManager 쪽에서 FinishReward 콜백을 호출해야 한다.
    /// </summary>
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

    /// <summary>
    /// 스킬 보상 처리.
    /// 
    /// 현재는 스킬 보상 UI가 아직 없으므로 바로 완료 처리한다.
    /// 나중에 스킬 선택 UI가 생기면 여기에서 연결하면 된다.
    /// </summary>
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
    
    /// <summary>
    /// 아이템 보상 처리.
    /// 
    /// 현재는 아이템 보상 생성/선택 시스템이 아직 없으므로 바로 완료 처리한다.
    /// 나중에 아이템 생성 또는 아이템 선택 UI를 여기에서 연결하면 된다.
    /// </summary>
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

    /// <summary>
    /// 보상 처리가 완전히 끝났을 때 호출된다.
    /// 
    /// 이 함수가 호출되어야 CombatRoomController의 OnRewardFinished가 실행되고,
    /// 다음 출구가 배정된 뒤 문이 열린다.
    /// </summary>
    private void FinishReward()
    {
        onRewardFinished?.Invoke();
        onRewardFinished = null;

        Destroy(gameObject);
    }

    /// <summary>
    /// 플레이어가 보상 오브젝트 상호작용 범위에 들어왔을 때.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerNear = true;
    }

    /// <summary>
    /// 플레이어가 보상 오브젝트 상호작용 범위에서 나갔을 때.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerNear = false;
    }
}