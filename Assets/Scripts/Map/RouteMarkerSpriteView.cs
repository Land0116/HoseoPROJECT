using TMPro;
using UnityEngine;

public class RouteMarkerSpriteView : MonoBehaviour
{
    [Header("아이콘")]
    [SerializeField] private SpriteRenderer iconRenderer;
    
    /// <summary>
    /// 출구 보상 표시 세팅.
    /// SpriteRenderer 방식이라 Canvas가 필요 없다.
    /// </summary>
    public void Setup(Sprite icon)
    {
        
        if (iconRenderer != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.enabled = icon != null;
        }
    }
}