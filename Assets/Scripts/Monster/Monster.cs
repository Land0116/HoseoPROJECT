using UnityEngine;

public class Monster : MonoBehaviour
{
    private Transform player;

    [Header("ÆÐÅÏ")]
    [SerializeField] private AttackPattern attackPattern;
    [SerializeField] private MovePattern movePattern;

    [Header("½ºÅÈ")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxHP = 50f;

    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float MaxHP => maxHP;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        attackPattern.Init(this);
        movePattern.Init(this);
    }

    void Update()
    {
        attackPattern.Execute();
        movePattern.Execute();
    }

}
