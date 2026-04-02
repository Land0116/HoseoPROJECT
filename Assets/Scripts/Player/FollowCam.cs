using System;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public static FollowCam Instance;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -15f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 외부에서 새 플레이어 Transform을 넣어줄 때 사용
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;
    }
}