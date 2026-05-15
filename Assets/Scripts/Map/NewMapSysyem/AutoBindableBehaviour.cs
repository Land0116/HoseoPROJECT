using UnityEngine;

/// <summary>
/// AutoBind 호출 흐름을 공통화하는 부모 클래스.
/// 
/// 이 클래스를 상속받는 스크립트는
/// AutoBindCore()만 override해서
/// 자기한테 필요한 참조 연결만 작성하면 된다.
/// 
/// 구조:
/// - AutoBind()              : 외부에서 호출하는 공식 자동 바인딩 함수
/// - AutoBindCore()          : 자식 클래스가 반드시 구현해야 하는 실제 바인딩 함수
/// - OnBeforeAutoBind()      : 바인딩 전 추가 작업이 필요할 때 사용
/// - OnAfterAutoBind()       : 바인딩 후 추가 작업이 필요할 때 사용
/// </summary>
public abstract class AutoBindableBehaviour : MonoBehaviour
{
    /// <summary>
    /// 런타임에서 오브젝트가 생성될 때 자동 바인딩한다.
    /// 
    /// 자식 클래스에서 Awake가 필요하면
    /// protected override void Awake()
    /// {
    ///     base.Awake();
    ///     ...
    /// }
    /// 형태로 사용하면 된다.
    /// </summary>
    protected virtual void Awake()
    {
        AutoBind();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 인스펙터 값이 바뀌거나 스크립트가 리컴파일될 때 자동 바인딩한다.
    /// 
    /// 에디터에서 하이라키 구조를 바꾼 뒤
    /// 자동으로 참조를 다시 잡는 용도다.
    /// </summary>
    protected virtual void OnValidate()
    {
        AutoBind();
    }

    /// <summary>
    /// 컴포넌트를 처음 붙였을 때 자동 바인딩한다.
    /// </summary>
    protected virtual void Reset()
    {
        AutoBind();
    }
#endif

    /// <summary>
    /// 인스펙터 우클릭 메뉴 또는 컴포넌트 메뉴에서 수동 실행 가능.
    /// 
    /// 모든 자동 바인딩은 이 함수 하나를 통해서만 실행한다.
    /// </summary>
    [ContextMenu("Auto Bind")]
    public void AutoBind()
    {
        OnBeforeAutoBind();

        AutoBindCore();

        OnAfterAutoBind();
    }

    /// <summary>
    /// 실제 자동 바인딩을 작성하는 함수.
    /// 
    /// 자식 클래스는 반드시 이 함수를 override해야 한다.
    /// </summary>
    protected abstract void AutoBindCore();

    /// <summary>
    /// 바인딩 전에 필요한 작업이 있으면 자식 클래스에서 override.
    /// 기본은 비워둔다.
    /// </summary>
    protected virtual void OnBeforeAutoBind()
    {
    }

    /// <summary>
    /// 바인딩 후에 필요한 작업이 있으면 자식 클래스에서 override.
    /// 기본은 비워둔다.
    /// </summary>
    protected virtual void OnAfterAutoBind()
    {
    }
}