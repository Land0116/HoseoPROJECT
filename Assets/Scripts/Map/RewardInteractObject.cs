using UnityEngine;
using UnityEngine.InputSystem;

public class RewardInteractObject : MonoBehaviour
{
    [System.Serializable]
    private class RewardIconData
    {
        [Header("보상 타입")]
        public StageClear.RewardType rewardType;

        [Header("이 보상 타입에 사용할 아이콘 Sprite")]
        public Sprite iconSprite;
    }

    [Header("아이콘을 보여줄 SpriteRenderer")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("보상 타입별 아이콘 Sprite")]
    [SerializeField] private RewardIconData[] iconDatas;

    private StageClear.RewardType rewardType = StageClear.RewardType.None;

    private bool isPlayerNear = false;
    private bool isOpened = false;

    private void Awake()
    {
        AutoBindIconRenderer();
    }

    private void Reset()
    {
        AutoBindIconRenderer();

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }

    public void Setup(StageClear.RewardType newRewardType)
    {
        rewardType = newRewardType;
        isOpened = false;

        ApplyRewardIcon(rewardType);
    }

    private void ApplyRewardIcon(StageClear.RewardType targetRewardType)
    {
        AutoBindIconRenderer();

        if (iconRenderer == null)
        {
            Debug.LogWarning("[RewardInteractObject] SpriteRenderer가 없음");
            return;
        }

        Sprite targetSprite = GetIconSprite(targetRewardType);

        if (targetSprite == null)
        {
            Debug.LogWarning($"[RewardInteractObject] {targetRewardType}에 해당하는 아이콘 Sprite가 없음");
            return;
        }

        // 핵심:
        // 오브젝트를 끄지 않고 SpriteRenderer의 Sprite만 교체한다.
        iconRenderer.enabled = true;
        iconRenderer.sprite = targetSprite;
    }

    private Sprite GetIconSprite(StageClear.RewardType targetRewardType)
    {
        if (iconDatas == null || iconDatas.Length == 0)
            return null;

        for (int i = 0; i < iconDatas.Length; i++)
        {
            if (iconDatas[i] == null) continue;

            if (iconDatas[i].rewardType == targetRewardType)
                return iconDatas[i].iconSprite;
        }

        return null;
    }

    private void AutoBindIconRenderer()
    {
        if (iconRenderer != null) return;

        // 현재 프리팹 루트에 SpriteRenderer가 붙어있다면 자동으로 잡음
        iconRenderer = GetComponent<SpriteRenderer>();

        // 혹시 자식에 붙어있는 구조라면 자식에서도 찾음
        if (iconRenderer == null)
            iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (!isPlayerNear) return;
        if (isOpened) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            isOpened = true;

            if (StageClear.Instance != null)
            {
                StageClear.Instance.OpenRewardUI(rewardType);
            }
            else
            {
                Debug.LogWarning("StageClear.Instance가 없어서 보상 UI를 열 수 없음");
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}