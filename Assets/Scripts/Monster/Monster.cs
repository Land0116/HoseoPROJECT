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




    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        currentHP = maxHP;

        GameObject ui = Instantiate(hpUIPrefab, transform);
        ui.transform.localPosition = new Vector3(0, 0.8f, 0); // 머리 위 위치

        hpUI = ui.GetComponent<MonsterHPUI>();
        hpUI.Init(this);



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
        
    }
    private void FixedUpdate()
    {
        if (attackPattern != null)
        {
            attackPattern.Execute();

            if (!attackPattern.blockMovement && movePattern != null)
            {
                movePattern.Execute();
            }
        }
    }

    //데미지 입음
    public void OnDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log("[" + currentHP + "]" + "남음");

        if (currentHP <= 0)
        {
            AugUIManager.instance.ShowAugmentation();
            Death();
        }
    }

    public void Death()
    {
        if (Random.value < weaponPrefabDropChance)
        {
            Instantiate(weaponPrefab, transform.position + weaponSpawner, Quaternion.identity);
        }
        if(Random.value < itemDropChance)
        {
            Instantiate(itemPrefab, transform.position + itemSpawner, Quaternion.identity);
        }
        Destroy(this.gameObject);
    }
}
