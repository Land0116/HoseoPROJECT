using System.Collections.Generic;
using UnityEngine;

public class OccluderMultiCircleFade2D : MonoBehaviour
{
    private const int MaxFadeTargets = 16;
    private const int MaxOverlapResults = 32;

    [Header("문 / 벽 스프라이트 루트")]
    [Tooltip("StageDoor, WallRoot처럼 실제 가려지는 SpriteRenderer들이 들어있는 루트")]
    [SerializeField] private Transform occluderRoot;

    [Header("페이드 대상 SpriteRenderer들")]
    [SerializeField] private SpriteRenderer[] targetRenderers;

    [Header("페이드용 머티리얼")]
    [SerializeField] private Material fadeMaterial;

    [Header("감지 트리거")]
    [Tooltip("문에 닿는 영역이 아니라, 플레이어/몬스터가 문 뒤에 가려질 가능성이 있는 전체 영역")]
    [SerializeField] private Collider2D fadeTrigger;

    [Header("감지 대상")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string monsterTag = "Monster";

    [Tooltip("Player, Monster 레이어를 체크. 비워두면 모든 레이어 검사")]
    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Header("원형 페이드 설정")]
    [SerializeField] private float fadeRadius = 1.6f;
    [SerializeField] private float fadeSoftness = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float innerAlpha = 0.18f;

    [Header("페이드 중심 보정")]
    [SerializeField] private Vector2 fadeCenterOffset = new Vector2(0f, 0.35f);
    [SerializeField] private bool useRendererBoundsCenter = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool drawGizmos = true;

    private readonly List<Transform> fadeTargets = new List<Transform>(MaxFadeTargets);
    private readonly Vector4[] fadeTargetPositions = new Vector4[MaxFadeTargets];
    private readonly Collider2D[] overlapResults = new Collider2D[MaxOverlapResults];

    private ContactFilter2D contactFilter;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int UseFadeID = Shader.PropertyToID("_UseFade");
    private static readonly int FadeRadiusID = Shader.PropertyToID("_FadeRadius");
    private static readonly int FadeSoftnessID = Shader.PropertyToID("_FadeSoftness");
    private static readonly int InnerAlphaID = Shader.PropertyToID("_InnerAlpha");
    private static readonly int FadeTargetCountID = Shader.PropertyToID("_FadeTargetCount");
    private static readonly int FadeTargetPositionsID = Shader.PropertyToID("_FadeTargetPositions");

    private void Awake()
    {
        AutoBind();

        propertyBlock = new MaterialPropertyBlock();

        SetupContactFilter();
        ApplyFadeMaterialIfNeeded();

        ClearFadeTargetPositions();
        SetFadeEnabled(false);

        PrintDebugState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBind();

        fadeRadius = Mathf.Max(0.01f, fadeRadius);
        fadeSoftness = Mathf.Max(0.01f, fadeSoftness);
    }

    private void Reset()
    {
        AutoBind();
    }
#endif

    private void AutoBind()
    {
        if (fadeTrigger == null)
        {
            fadeTrigger = GetComponent<Collider2D>();
        }

        if (fadeTrigger != null)
        {
            fadeTrigger.isTrigger = true;
        }

        if (occluderRoot == null)
        {
            occluderRoot = transform;
        }

        if ((targetRenderers == null || targetRenderers.Length == 0) && occluderRoot != null)
        {
            targetRenderers = occluderRoot.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    private void SetupContactFilter()
    {
        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = targetLayerMask;
    }

    private void ApplyFadeMaterialIfNeeded()
    {
        if (fadeMaterial == null)
        {
            Debug.LogWarning($"[OccluderFade] {name} FadeMaterial이 없음. M_MultiCircleFade를 넣어야 함.");
            return;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning($"[OccluderFade] {name} TargetRenderers가 없음. OccluderRoot를 StageDoor/WallRoot로 넣어.");
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer sr = targetRenderers[i];
            if (sr == null) continue;

            sr.sharedMaterial = fadeMaterial;
        }
    }

    private void LateUpdate()
    {
        RefreshFadeTargetsByOverlap();

        if (fadeTargets.Count <= 0)
        {
            ClearFadeTargetPositions();
            SetFadeEnabled(false);
            return;
        }

        UpdateFadeTargetPositions();
        ApplyFadeToRenderers();
    }

    private void RefreshFadeTargetsByOverlap()
    {
        fadeTargets.Clear();

        if (fadeTrigger == null)
            return;

        int count = fadeTrigger.Overlap(contactFilter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit == null) continue;

            if (!IsValidFadeTarget(hit))
                continue;

            Transform target = GetTargetRoot(hit);
            if (target == null) continue;

            if (fadeTargets.Contains(target))
                continue;

            if (fadeTargets.Count >= MaxFadeTargets)
                break;

            fadeTargets.Add(target);
        }

        if (debugLog && fadeTargets.Count > 0)
        {
            // 너무 많이 찍히면 꺼도 됨
            // Debug.Log($"[OccluderFade] {name} Current Targets: {fadeTargets.Count}");
        }
    }

    private bool IsValidFadeTarget(Collider2D collision)
    {
        if (collision == null)
            return false;

        if (collision.CompareTag(playerTag))
            return true;

        if (collision.CompareTag(monsterTag))
            return true;

        Transform root = GetTargetRoot(collision);

        if (root == null)
            return false;

        if (root.CompareTag(playerTag))
            return true;

        if (root.CompareTag(monsterTag))
            return true;

        return false;
    }

    private Transform GetTargetRoot(Collider2D collision)
    {
        if (collision == null)
            return null;

        if (collision.attachedRigidbody != null)
            return collision.attachedRigidbody.transform;

        return collision.transform.root;
    }

    private void UpdateFadeTargetPositions()
    {
        ClearFadeTargetPositions();

        int count = Mathf.Min(fadeTargets.Count, MaxFadeTargets);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetFadeCenter(fadeTargets[i]);
            fadeTargetPositions[i] = new Vector4(pos.x, pos.y, 0f, 0f);
        }
    }

    private Vector3 GetFadeCenter(Transform target)
    {
        if (target == null)
            return transform.position;

        Vector3 center = target.position;

        if (useRendererBoundsCenter)
        {
            SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
            {
                center = renderer.bounds.center;
                return center + (Vector3)fadeCenterOffset;
            }
        }

        Collider2D col = target.GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            center = col.bounds.center;
            return center + (Vector3)fadeCenterOffset;
        }

        return target.position + (Vector3)fadeCenterOffset;
    }

    private void ClearFadeTargetPositions()
    {
        for (int i = 0; i < MaxFadeTargets; i++)
        {
            fadeTargetPositions[i] = Vector4.zero;
        }
    }

    private void ApplyFadeToRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        int count = Mathf.Min(fadeTargets.Count, MaxFadeTargets);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer sr = targetRenderers[i];

            if (sr == null)
                continue;

            sr.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(UseFadeID, 1f);
            propertyBlock.SetFloat(FadeRadiusID, fadeRadius);
            propertyBlock.SetFloat(FadeSoftnessID, fadeSoftness);
            propertyBlock.SetFloat(InnerAlphaID, innerAlpha);
            propertyBlock.SetInt(FadeTargetCountID, count);
            propertyBlock.SetVectorArray(FadeTargetPositionsID, fadeTargetPositions);

            sr.SetPropertyBlock(propertyBlock);
        }
    }

    private void SetFadeEnabled(bool enabled)
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        float useFadeValue = enabled ? 1f : 0f;
        int count = enabled ? Mathf.Min(fadeTargets.Count, MaxFadeTargets) : 0;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer sr = targetRenderers[i];

            if (sr == null)
                continue;

            sr.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(UseFadeID, useFadeValue);
            propertyBlock.SetFloat(FadeRadiusID, fadeRadius);
            propertyBlock.SetFloat(FadeSoftnessID, fadeSoftness);
            propertyBlock.SetFloat(InnerAlphaID, innerAlpha);
            propertyBlock.SetInt(FadeTargetCountID, count);
            propertyBlock.SetVectorArray(FadeTargetPositionsID, fadeTargetPositions);

            sr.SetPropertyBlock(propertyBlock);
        }
    }

    private void PrintDebugState()
    {
        if (!debugLog)
            return;

        Debug.Log(
            $"[OccluderFade] Init / Object={name}" +
            $" / OccluderRoot={(occluderRoot == null ? "NULL" : occluderRoot.name)}" +
            $" / TargetRenderers={(targetRenderers == null ? 0 : targetRenderers.Length)}" +
            $" / FadeMaterial={(fadeMaterial == null ? "NULL" : fadeMaterial.name)}" +
            $" / FadeTrigger={(fadeTrigger == null ? "NULL" : fadeTrigger.name)}"
        );
    }

    private void OnDisable()
    {
        fadeTargets.Clear();
        ClearFadeTargetPositions();
        SetFadeEnabled(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);

        if (fadeTargets != null && fadeTargets.Count > 0)
        {
            for (int i = 0; i < fadeTargets.Count; i++)
            {
                if (fadeTargets[i] == null)
                    continue;

                Vector3 center = Application.isPlaying
                    ? GetFadeCenter(fadeTargets[i])
                    : fadeTargets[i].position + (Vector3)fadeCenterOffset;

                Gizmos.DrawWireSphere(center, fadeRadius);
            }
        }
        else
        {
            if (fadeTrigger != null)
                Gizmos.DrawWireCube(fadeTrigger.bounds.center, fadeTrigger.bounds.size);

            Gizmos.DrawWireSphere(transform.position, fadeRadius);
        }
    }
}