using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData")]
public class PlayerData : ScriptableObject
{
    private static PlayerData _instance;

    public enum AttackType
    {
        Base, //단추
        Item //아이템
    }

    public enum PlayerState
    {
        Idle, //대기
        Walk, //걷기
        Attack, //타격
        Hit, //피격
        Death //죽음
    }
    
    [Header("플레이어 정보")]
    [Header("플레이어 체력")] public int Hp; //playerHP
    [Header("플레이어 최대체력")] public int MaxHp; //playerHP
    [Header("플레이어 소지금")] public int Gold; //playerGold
    [Header("플레이어 이동속도")] public float MoveSpeed; //playerMoveSpeed;
    [Header("플레이어 데미지")] public float Damage; //playerDamage;
    [Header("플레이어 총알 현재 개수")] public int Amount;
    [Header("플레이어 총알 최대 개수")] public int MaxAmount;
    
    [Header("플레이어의 상태")] 
    [Header("플레이어 상태")] public PlayerState playerState;
    [Header("플레이어 공격타입")] public AttackType playerAttackType;
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
