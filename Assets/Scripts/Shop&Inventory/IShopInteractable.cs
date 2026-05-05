using UnityEngine;

public interface IShopInteractable
{
    Transform GetTransform();
    void Interact(PlayerController player);
}