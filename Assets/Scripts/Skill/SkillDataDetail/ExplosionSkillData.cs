using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionSkill", menuName = "Game/Skill/Explosion")]
public class ExplosionSkillData : SkillData
{
    public GameObject explosionPrefab;

    public override void Execute(GameObject caster)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player == null) return;

        int level = GetSkillLevel();

        float finalDamage = CalculateDamage(player, level);

        // 공통 함수 사용
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        GameObject explosion = Instantiate(explosionPrefab, mouseWorldPos, Quaternion.identity);

        ExplosionSkillInstance instance = explosion.GetComponent<ExplosionSkillInstance>();

        if (instance != null)
        {
            instance.Init(finalDamage, GetRange(level), GetDuration(level));
        }
    }

    private int GetSkillLevel()
    {
        if (SkillManager.Instance.qSkill == this)
            return SkillManager.Instance.GetQLevel();

        if (SkillManager.Instance.eSkill == this)
            return SkillManager.Instance.GetELevel();

        return 1;
    }

    private float CalculateDamage(PlayerController player, int level)
    {
        float playerDamage = player.GetFinalDamage();

        switch (level)
        {
            case 1: return 20f + playerDamage * 2f;
            case 2: return 25f + playerDamage * 2.2f;
            case 3: return 30f + playerDamage * 2.5f;
        }

        return 20f;
    }

    private float GetRange(int level)
    {
        return 3f;
    }

    private float GetDuration(int level)
    {
        return 0.1f;
    }

    public override float GetCooldown(int level)
    {
        switch (level)
        {
            case 1: return 5f;
            case 2: return 5f;
            case 3: return 4.5f;
        }
        return 5f;
    }

    public override float GetDamageMultiplier(int level)
    {
        switch (level)
        {
            case 1: return 2f;
            case 2: return 2.2f;
            case 3: return 2.5f;
        }
        return 2f;
    }

    public override float GetFinalDamage(PlayerController player, int level)
    {
        switch (level)
        {
            case 1: return 20f + player.GetFinalDamage() * 2f;
            case 2: return 25f + player.GetFinalDamage() * 2.2f;
            case 3: return 30f + player.GetFinalDamage() * 2.5f;
        }
        return 20f;
    }
}