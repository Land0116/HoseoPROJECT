using System.Collections;
using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    private Transform player;

    [Header("패턴")]
    [SerializeField] private AttackPattern attackPattern;
    [SerializeField] private MovePattern movePattern;

    [Header("스탯")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxHP = 50f;

    [Header("사망시 골드")]
    [SerializeField] private int minGold = 1;
    [SerializeField] private int maxGold = 3;

    [SerializeField] private GameObject hpUIPrefab;
    private MonsterHPUI hpUI;

    private float currentHP;

    public float CurrentHP => currentHP;
    private Rigidbody2D rb;
    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float MaxHP => maxHP;
    public Rigidbody2D RB => rb;

    [Header("아이템 드랍세팅 0.1은 10퍼")]
    [SerializeField]
    private float weaponPrefabDropChance = 0.3f;
    [SerializeField]
    private float itemDropChance = 0.2f;
    [SerializeField]
    private GameObject weaponPrefab;
    [SerializeField]
    private GameObject itemPrefab;


    [SerializeField]
    private Vector3 weaponSpawner = new Vector3(-0.5f, 0, 0);
    [SerializeField]
    private Vector3 itemSpawner = new Vector3(0.5f, 0, 0);
    
    
    // 플레이어를 기다리는 코루틴 저장용
    private Coroutine bindPlayerRoutine;

    private void OnEnable()
    {
        // 몬스터가 활성화될 때
        // 플레이어가 아직 생성되지 않았을 수도 있으므로
        // 코루틴으로 PlayerController.Instance가 생길 때까지 기다림
        bindPlayerRoutine = StartCoroutine(BindPlayerRoutine());
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되거나 파괴될 때
        // 코루틴이 남아 있으면 정리
        if (bindPlayerRoutine != null)
        {
            StopCoroutine(bindPlayerRoutine);
            bindPlayerRoutine = null;
        }
    }


    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        currentHP = maxHP;

        if (hpUIPrefab != null)
        {
            GameObject ui = Instantiate(hpUIPrefab, transform);
            ui.transform.localPosition = new Vector3(0, 0.8f, 0);

            hpUI = ui.GetComponent<MonsterHPUI>();

            if (hpUI != null)
            {
                hpUI.Init(this);
            }
        }

        
        if (attackPattern != null)
        {
            attackPattern.Init(this);
        }
        if (movePattern)
        {
            movePattern.Init(this);
        }
    }

    void Update()
    {
        // 플레이어가 씬 전환/삭제/재생성 때문에 잠깐 null 될 수 있으니
        // null이면 다시 연결 시도
        if (player == null && PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
    }
    
    private void FixedUpdate()
    {
        if (player == null) return;
        if (attackPattern != null)
        {
            attackPattern.Execute();

            if (!attackPattern.blockMovement && movePattern != null)
            {
                movePattern.Execute();
            }
        }
    }
    
    /// <summary>
    /// 플레이어가 준비될 때까지 기다렸다가 연결하는 코루틴
    /// </summary>
    private IEnumerator BindPlayerRoutine()
    {
        // PlayerController.Instance가 생길 때까지 대기
        while (PlayerController.Instance == null)
        {
            yield return null;
        }

        // 플레이어가 준비되면 Transform 연결
        player = PlayerController.Instance.transform;
    }

    //데미지 입음
    public void OnDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log("[" + currentHP + "]" + "남음");

        if (currentHP <= 0)
        {
            //AugUIManager.instance.ShowAugmentation();
            Death();
        }
    }

    public void Death()
    {

        if (PlayerController.Instance != null)
        {
            int rewardGold = Random.Range(minGold, maxGold + 1);
            PlayerController.Instance.Gold += rewardGold;
            Debug.Log("골드 획득: " + rewardGold);
        }

        if (Random.value < weaponPrefabDropChance)
        {
            Instantiate(weaponPrefab, transform.position + weaponSpawner, Quaternion.identity);
        }
        if(Random.value < itemDropChance)
        {
            Instantiate(itemPrefab, transform.position + itemSpawner, Quaternion.identity);
        }
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnKillEnemy();
        }
        Destroy(this.gameObject);
    }
}
