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
    
    [Header("플레이어 정보")]
    [Header("플레이어 체력")] public int Hp = 50; //playerHP
    [Header("플레이어 소지금")] public int Gold; //playerGold
    [Header("플레이어 이동속도")] public float MoveSpeed; //playerMoveSpeed;
    [Header("플레이어 데미지")] public float Damage; //playerDamage;
    [Header("플레이어 총알 현재 개수")] public int Amount;
    [Header("플레이어 총알 최대 개수")] public int MaxAmount;
    
    [Header("")] 
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
