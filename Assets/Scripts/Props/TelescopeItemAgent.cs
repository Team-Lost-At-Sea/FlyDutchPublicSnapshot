using UnityEngine;

public class TelescopeItemAgent : MonoBehaviour
{
    ActiveItem activeItem; // The active item that controls the telescope zoom
    TelescopeZoom telescopeZoom; // Reference to the TelescopeZoom component

    void OnEnable()
    {
        ActiveItem activeItem = GetComponent<ActiveItem>();
        telescopeZoom = GetComponent<TelescopeZoom>(); // Get the TelescopeZoom component
        activeItem.OnEquip += HandleEquip; // Subscribe to the equip event
        activeItem.OnUnequip += HandleUnequip; // Subscribe to the unequip event
    }

    void OnDisable()
    {
        if (activeItem != null)
        {
            activeItem.OnEquip -= HandleEquip; // Unsubscribe from the equip event
            activeItem.OnUnequip -= HandleUnequip; // Unsubscribe from the unequip event
        }
    }

    void HandleEquip()
    {
        telescopeZoom.enabled = true; // Enable the telescope zoom functionality
    }

    void HandleUnequip()
    {
        telescopeZoom.ResetFOV(); // Reset the FOV to default when unequipped
        telescopeZoom.enabled = false; // Disable the telescope zoom functionality
    }
}
