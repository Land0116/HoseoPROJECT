using UnityEngine;


[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    [TextArea] public string description;

    [Header("아이콘")]
    public Sprite icon;

    [Header("레벨")]
    public int maxLevel = 3;

    [Header("스텟")]
    public float baseDamage;
    public float damageMutiplier;
    public float range;
    public float duration;
    public float cooldown;

    [Header("기타")]
    public int price;

    public virtual void Execute(GameObject caster)
    {
        Debug.Log($"{skillName} 사용됨");
    }
    public virtual float GetCooldown(int level)
    {
        return cooldown;
    }

    public virtual float GetDamageMultiplier(int level)
    {
        return damageMutiplier;
    }

    public virtual float GetFinalDamage(PlayerController player, int level)
    {
        return baseDamage + player.GetFinalDamage() * GetDamageMultiplier(level);
    }

    protected Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mouseScreen);
        world.z = 0f;
        return world;
    }

}
