using System;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 inputDirection;
    
    [Header("조준점 설정")]
    [SerializeField] private Transform crosshairTransform; // 계층 구조의 Crosshair 오브젝트 연결
    [SerializeField] private bool hideSystemCursor = true; // 시스템 커서 숨김 여부
    [SerializeField] private Transform playerBody;
    
    private Camera _mainCamera;
    
    public PlayerData playerData;
    
    [Header("공격 관련")]
    public GameObject bulletPrefab;
    public Transform gunTip;
    public float range;
    public float reloadTime = 1.5f; // 장전 걸리는 시간
    private bool isReloading = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (playerData == null)
        {
            // Resources 폴더에서 해당 이름의 에셋을 찾아 할당합니다.
            playerData = Resources.Load<PlayerData>("player/playerBaseData");
        }
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0f; 
        
        _mainCamera = Camera.main;
        playerData.Amount = playerData.MaxAmount;
    }

    void Start()
    {
        if (hideSystemCursor) Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // UpdateCrosshairPosition();
        // RotatePlayerToMouse();

        PlayerMouseMovement();

    }
    
    
    void FixedUpdate()
    {
        Vector2 moveVector = inputDirection;
        if (moveVector.magnitude > 1) moveVector.Normalize();
        
        rb.linearVelocity = moveVector * playerData.MoveSpeed;
    }

    void PlayerMouseMovement()
    {
        if (crosshairTransform == null || playerBody == null) return;

        // 1. 마우스 월드 좌표 계산 (한 번만 수행)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x, 
            mouseScreenPos.y, 
            -_mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;

        // 2. 조준점 위치 업데이트
        crosshairTransform.position = mouseWorldPos;

        // 3. 플레이어 회전 계산 (조준점 위치를 바로 활용)
        Vector2 direction = ((Vector2)mouseWorldPos - (Vector2)playerBody.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        playerBody.rotation = Quaternion.Euler(0, 0, angle);
        
    }
    
    //단추(기본공격) 발사
    void Shoot()
    {
        playerData.Amount--;
        Instantiate(bulletPrefab, gunTip.position, playerBody.rotation);
    }
    
    //유니티 기본 InputSystem
    private void OnMove(InputValue movementValue)
    {
        inputDirection = movementValue.Get<Vector2>();
        //playerData.playerState = PlayerData.PlayerState.Walk;
    }
    private void OnAttack(InputValue value)
    {
        // 버튼을 '눌렀을 때' / 단추의 개수가 0보다 클 때 실행 (떼거나 유지할 때 중복 실행 방지)
        if (value.isPressed && playerData.Amount > 0 && !isReloading)
        {
            Shoot();
        }
    }

    private void OnReload(InputValue value)
    {
        if (value.isPressed && !isReloading)
        {
            StartCoroutine(Reload());
        }
        else if (isReloading)
        {
            Debug.Log("장전 중입니다.");
        }
    }
    
    //기본 공격총알 장전
    IEnumerator Reload()
    {
        isReloading = true;
        
        yield return new WaitForSeconds(reloadTime);
        playerData.Amount = playerData.MaxAmount;
        isReloading = false;
    }
    
}
