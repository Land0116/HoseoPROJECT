using System.Collections.Generic;
using UnityEngine;

public class OccluderMultiCircleFade2D : MonoBehaviour
{
    private const int MaxFadeTargets = 16;

    [Header("페이드 대상 SpriteRenderer들")]
    [SerializeField] private SpriteRenderer[] targetRenderers;

    [Header("페이드용 머티리얼")]
    [SerializeField] private Material fadeMaterial;

    [Header("감지 트리거")]
    [SerializeField] private Collider2D fadeTrigger;

    [Header("감지 대상 태그")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string monsterTag = "Monster";

    [Header("원형 페이드 설정")]
    [SerializeField] private float fadeRadius = 1.2f;

    [SerializeField] private float fadeSoftness = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float innerAlpha = 0.25f;

    [Header("디버그")]
    [SerializeField] private bool drawGizmos = true;

    private readonly List<Transform> fadeTargets = new List<Transform>(MaxFadeTargets);
    private readonly Vector4[] fadeTargetPositions = new Vector4[MaxFadeTargets];

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
        ApplyFadeMaterialIfNeeded();

        propertyBlock = new MaterialPropertyBlock();

        SetFadeEnabled(false);
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
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (fadeTrigger == null)
        {
            fadeTrigger = GetComponent<Collider2D>();
        }

        if (fadeTrigger != null)
        {
            fadeTrigger.isTrigger = true;
        }
    }

    private void ApplyFadeMaterialIfNeeded()
    {
        if (fadeMaterial == null)
            return;

        if (targetRenderers == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer sr = targetRenderers[i];

            if (sr == null)
                continue;

            sr.sharedMaterial = fadeMaterial;
        }
    }

    private void LateUpdate()
    {
        RemoveNullTargets();

        if (fadeTargets.Count <= 0)
        {
            SetFadeEnabled(false);
            return;
        }

        UpdateFadeTargetPositions();
        ApplyFadeToRenderers();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsValidFadeTarget(collision))
            return;

        Transform target = GetTargetRoot(collision);

        if (target == null)
            return;

        if (fadeTargets.Contains(target))
            return;

        if (fadeTargets.Count >= MaxFadeTargets)
            return;

        fadeTargets.Add(target);

        SetFadeEnabled(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Transform target = GetTargetRoot(collision);

        if (target == null)
            return;

        fadeTargets.Remove(target);

        if (fadeTargets.Count <= 0)
        {
            SetFadeEnabled(false);
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

        // 몬스터의 자식 콜라이더가 들어오는 경우 대비
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

        // Rigidbody2D가 있으면 그 Transform을 기준으로 사용
        if (collision.attachedRigidbody != null)
            return collision.attachedRigidbody.transform;

        return collision.transform;
    }

    private void RemoveNullTargets()
    {
        for (int i = fadeTargets.Count - 1; i >= 0; i--)
        {
            if (fadeTargets[i] == null)
            {
                fadeTargets.RemoveAt(i);
            }
        }
    }

    private void UpdateFadeTargetPositions()
    {
        for (int i = 0; i < MaxFadeTargets; i++)
        {
            fadeTargetPositions[i] = Vector4.zero;
        }

        int count = Mathf.Min(fadeTargets.Count, MaxFadeTargets);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = fadeTargets[i].position;
            fadeTargetPositions[i] = new Vector4(pos.x, pos.y, 0f, 0f);
        }
    }

    private void ApplyFadeToRenderers()
    {
        if (targetRenderers == null)
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
        if (targetRenderers == null)
            return;

        float useFadeValue = enabled ? 1f : 0f;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer sr = targetRenderers[i];

            if (sr == null)
                continue;

            sr.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(UseFadeID, useFadeValue);
            propertyBlock.SetInt(FadeTargetCountID, enabled ? fadeTargets.Count : 0);
            propertyBlock.SetFloat(FadeRadiusID, fadeRadius);
            propertyBlock.SetFloat(FadeSoftnessID, fadeSoftness);
            propertyBlock.SetFloat(InnerAlphaID, innerAlpha);
            propertyBlock.SetVectorArray(FadeTargetPositionsID, fadeTargetPositions);

            sr.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnDisable()
    {
        fadeTargets.Clear();
        SetFadeEnabled(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);

        if (fadeTargets != null && fadeTargets.Count > 0)
        {
            for (int i = 0; i < fadeTargets.Count; i++)
            {
                if (fadeTargets[i] == null)
                    continue;

                Gizmos.DrawWireSphere(fadeTargets[i].position, fadeRadius);
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, fadeRadius);
        }
    }
}