using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public abstract void Use(GameObject user);
}