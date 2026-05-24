using UnityEngine;


[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill")]
public class SkillData : ScriptableObject
{
    [Header("?? ????")]
    public string skillName;
    [TextArea] public string description;

    [Header("??????")]
    public Sprite icon;
    public Sprite sloticon;
    [Header("????")]
    public int maxLevel = 3;

    [Header("????")]
    public float baseDamage;
    public float damageMutiplier;
    public float range;
    public float duration;
    public float cooldown;

    [Header("???")]
    public int price;

    [Header("sound")]
    public AudioClip skillSound;
    [Header("Sound Delay")]
    public float soundDelay = 0f;

    public virtual void Execute(GameObject caster)
    {
        Debug.Log($"{skillName}");
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

    public virtual void PlaySound(MonoBehaviour runner, AudioSource source)
    {
        if (skillSound == null || source == null || runner == null) return;

        if (soundDelay <= 0f)
        {
            source.PlayOneShot(skillSound);
        }
        else
        {
            runner.StartCoroutine(PlaySoundDelayed(source));
        }
    }

    private System.Collections.IEnumerator PlaySoundDelayed(AudioSource source)
    {
        yield return new WaitForSeconds(soundDelay);

        if (source != null)
            source.PlayOneShot(skillSound);
    }

}
