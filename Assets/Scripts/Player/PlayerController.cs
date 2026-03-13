using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour, IDamageable
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 inputDirection;
    
    [Header("조준점 설정")]
    [SerializeField] private Transform crosshairTransform; // 계층 구조의 Crosshair 오브젝트 연결
    [SerializeField] private bool hideSystemCursor = true; // 시스템 커서 숨김 여부
    [SerializeField] private Transform playerBody;
    
    private Camera _mainCamera;
    
    public PlayerData playerData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (playerData == null)
        {
            // Resources 폴더에서 해당 이름의 에셋을 찾아 할당합니다.
            playerData = Resources.Load<PlayerData>("player/playerBaseData");
        }

        playerData = Instantiate(playerData);

        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0f; 
        
        _mainCamera = Camera.main;
        
    }

    void Start()
    {
        if (hideSystemCursor) Cursor.visible = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCrosshairPosition();
        RotatePlayerToMouse();
    }
    void FixedUpdate()
    {
        Vector2 moveVector = inputDirection;
        if (moveVector.magnitude > 1) moveVector.Normalize();
        
        rb.linearVelocity = moveVector * playerData.MoveSpeed;
    }
    
    void UpdateCrosshairPosition()
    {
        if (crosshairTransform == null) return;
        
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        // 카메라와의 거리를 고려하여 좌표 변환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -_mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;
        
        crosshairTransform.position = mouseWorldPos;
        
        crosshairTransform.position = mouseWorldPos;
    }

    void RotatePlayerToMouse()
    {
        if (playerBody == null) return;

        // 1. 플레이어에서 조준점(마우스)으로 향하는 방향 벡터 구하기
        Vector2 direction = (crosshairTransform.position - playerBody.position).normalized;

        // 2. Atan2를 사용하여 각도(Radian -> Degree) 계산
        // 탄젠트의 역함수로, y와 x값을 넣어주면 -180 ~ 180도 사이의 각도를 반환합니다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 3. 계산된 각도만큼 Z축 회전 적용
        // 기본 스프라이트가 오른쪽(Right)을 바라보고 있다면 그대로 사용하면 됩니다.
        playerBody.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    public void OnDamage(float damage)
    {
        playerData.Hp -= (int)damage;
        Debug.Log("남은 유저 체력: " + playerData.Hp);

        if(playerData.Hp <= 0)
        {
            Die();
        }
    }
    private void OnMove(InputValue movementValue)
    {
        inputDirection = movementValue.Get<Vector2>();
        Debug.Log("나 이동해");
    }
    void Die()
    {
        Debug.Log("플레이어 사망");
    }
    

}
