using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "ItemEffect/AreaDamage")]
public class AreaDamageEffect : ItemEffect
{
    public GameObject damageAreaPrefab;

    public override void Use(GameObject user)
    {
        if (Camera.main == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Instantiate(damageAreaPrefab, mouseWorldPos, Quaternion.identity);
    }
}