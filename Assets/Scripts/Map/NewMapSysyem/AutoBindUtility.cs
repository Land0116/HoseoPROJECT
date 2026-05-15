using UnityEngine;

/// <summary>
/// 자동 바인딩에서 공통으로 사용하는 탐색 도구 클래스.
/// 
/// 주의:
/// 이 클래스는 static 클래스이기 때문에 상속/override 대상이 아니다.
/// 즉, AutoBindUtility 안에 virtual AutoBind()를 만들 수 없다.
/// 
/// 역할:
/// - 자식 오브젝트 이름으로 찾기
/// - 이름이 특정 문자열로 시작하는 자식 찾기
/// - 특정 이름의 자식에서 컴포넌트 가져오기
/// - 플레이어 Transform 찾기
/// - 배열에 null이 있는지 검사하기
/// </summary>
public static class AutoBindUtility
{
    /// <summary>
    /// parent 아래의 모든 자식 Transform을 재귀적으로 돌면서
    /// 이름이 targetName과 정확히 같은 Transform을 찾는다.
    /// 
    /// 예:
    /// FindChildRecursive(transform, "BlockObject");
    /// </summary>
    public static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }

            Transform result = FindChildRecursive(child, targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// parent 아래의 모든 자식 Transform을 재귀적으로 돌면서
    /// 이름이 startsWithName으로 시작하는 Transform을 찾는다.
    /// 
    /// 예:
    /// markerSpawnPoint_A
    /// markerSpawnPoint_B
    /// 같은 이름 구조를 쓸 때 유용하다.
    /// </summary>
    public static Transform FindChildStartsWith(Transform parent, string startsWithName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(startsWithName))
            {
                return child;
            }

            Transform result = FindChildStartsWith(child, startsWithName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// 특정 이름의 자식을 찾은 뒤,
    /// 그 오브젝트에서 원하는 컴포넌트 T를 가져온다.
    /// 
    /// 예:
    /// Button button = FindComponentInChildByName<Button>(root, "StartButton");
    /// </summary>
    public static T FindComponentInChildByName<T>(Transform parent, string targetName) where T : Component
    {
        Transform target = FindChildRecursive(parent, targetName);

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<T>();
    }

    /// <summary>
    /// 배열이 비어 있거나,
    /// 배열 안에 Missing/null 요소가 하나라도 있는지 검사한다.
    /// 
    /// 맵 시스템에서는 GateController 배열이 Missing이 되면
    /// 문 열기/닫기에서 NullReferenceException이 날 수 있으므로
    /// 자동 재바인딩 조건으로 사용한다.
    /// </summary>
    public static bool IsNullOrEmptyOrContainsNull<T>(T[] array) where T : Object
    {
        if (array == null) return true;
        if (array.Length == 0) return true;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 현재 씬에서 플레이어 Transform을 찾는다.
    /// 
    /// 1순위:
    /// PlayerController.Instance
    /// 
    /// 2순위:
    /// Player 태그 검색
    /// 
    /// 주의:
    /// 에디터 상태에서는 플레이어가 없을 수 있으므로
    /// 보통 Application.isPlaying일 때만 호출하는 게 안전하다.
    /// </summary>
    public static Transform FindPlayerTransform()
    {
        if (PlayerController.Instance != null)
        {
            return PlayerController.Instance.transform;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            return playerObject.transform;
        }

        return null;
    }
}