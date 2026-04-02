using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(WallColliderGenerator))]
public class WallColliderGenerator2DEditor : Editor
{
    WallColliderGenerator wall;

    private void OnEnable()
    {
        wall = target as WallColliderGenerator;

        // 인스펙터에서 LineRenderer 바로 아래로 올려서 보기 편하게
        UnityEditorInternal.ComponentUtility.MoveComponentUp(wall);

        wall.OnEnableCallback();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        // 두께
        wall.radius = EditorGUILayout.FloatField("Radius", wall.radius);

        if (EditorGUI.EndChangeCheck())
        {
            wall.UpdateRadius();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Collider"))
            wall.ClearCollider();

        if (GUILayout.Button("Create Collider"))
            wall.CreateCollider();
    }
}
#endif

[RequireComponent(typeof(LineRenderer))]
public class WallColliderGenerator : MonoBehaviour
{
    [Header("벽 두께의 반지름")]
    public float radius = 0.5f;

    // 실제 두께
    private float Diameter => radius * 2f;

    private LineRenderer lineRenderer;

    /// <summary>
    /// 에디터에서 컴포넌트가 활성화될 때 LineRenderer 기본 설정
    /// </summary>
    public void OnEnableCallback()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 2D에서도 라인을 보이게 하기 위한 기본 설정
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = Diameter;
        lineRenderer.endWidth = Diameter;
        lineRenderer.numCornerVertices = 10;
        lineRenderer.numCapVertices = 10;

        // 머티리얼이 없으면 기본 스프라이트 머티리얼 사용
        if (lineRenderer.sharedMaterial == null)
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = Color.gray;
            lineRenderer.sharedMaterial = material;
        }
    }

    /// <summary>
    /// 생성된 2D 콜라이더 전부 제거
    /// - 현재 오브젝트에 붙은 CircleCollider2D 제거
    /// - 자식으로 만들어둔 BoxCollider2D 오브젝트 제거
    /// </summary>
    public void ClearCollider()
    {
        // 현재 오브젝트에 붙어 있는 CircleCollider2D 제거
        Array.ForEach(GetComponents<CircleCollider2D>(), x => DestroyImmediate(x));

        // 자식으로 만들어둔 Box 오브젝트 제거
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    /// <summary>
    /// LineRenderer 포인트를 기준으로 2D 벽 콜라이더 생성
    /// - 각 점마다 CircleCollider2D 생성
    /// - 점과 점 사이를 BoxCollider2D로 연결
    /// </summary>
    public void CreateCollider()
    {
        ClearCollider();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        int size = lineRenderer.positionCount;

        // 점이 2개 미만이면 선분이 없어서 벽 생성 불가
        if (size < 2) return;

        List<Vector3> points = new List<Vector3>();

        // 1. 각 포인트 위치 저장 + 각 점에 CircleCollider2D 생성
        for (int i = 0; i < size; i++)
        {
            Vector3 point = lineRenderer.GetPosition(i);
            points.Add(point);

            // 각 점마다 둥근 끝 처리를 위한 CircleCollider2D 생성
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.offset = point;
            circle.radius = radius;
        }

        // 2. 점과 점 사이를 BoxCollider2D로 연결
        for (int i = 0; i < size - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];

            // 자식 오브젝트로 Box 생성
            GameObject boxObj = new GameObject("Box");
            boxObj.transform.SetParent(transform);
            boxObj.transform.localPosition = (start + end) * 0.5f;

            // 방향 벡터
            Vector2 dir = (end - start);

            // 두 점 사이 거리
            float distance = dir.magnitude;

            // 2D에서는 Z축 회전만 필요
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            boxObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // BoxCollider2D 추가
            BoxCollider2D box = boxObj.AddComponent<BoxCollider2D>();

            // X축 길이 = 두 점 사이 거리
            // Y축 두께 = 지름
            box.size = new Vector2(distance, Diameter);
        }
    }

    /// <summary>
    /// 반지름 변경 시
    /// - CircleCollider2D 반지름 갱신
    /// - BoxCollider2D 두께 갱신
    /// - LineRenderer 두께 갱신
    /// </summary>
    public void UpdateRadius()
    {
        // 점 콜라이더 반지름 갱신
        Array.ForEach(GetComponents<CircleCollider2D>(), x =>
        {
            x.radius = radius;
        });

        // 선분 박스 두께 갱신
        for (int i = 0; i < transform.childCount; i++)
        {
            BoxCollider2D box = transform.GetChild(i).GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Vector2 size = box.size;
                size.y = Diameter;
                box.size = size;
            }
        }

        // 라인 두께 갱신
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = Diameter;
        lineRenderer.endWidth = Diameter;
    }
}