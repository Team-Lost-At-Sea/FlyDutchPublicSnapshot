using UnityEngine;
using Needle.Console;

public class TelescopeZoom : MonoBehaviour
{
    private Camera playerCamera; // The camera used for the telescope view
    private ActiveItem activeItem; // (Kept here in case you need it elsewhere, but unused for zooming now)
    public float zoomFOV = 30f; // The zoomed-in field of view for the telescope
    public float zoomSpeed = 10f; // Speed of zoom transition
    private float defaultFOV; // Default field of view to reset to
    private float targetFOV; // Current target FOV to lerp toward
    private PlayerCharacterController pcc; // Reference to player controller

    void Awake()
    {
        activeItem = GetComponent<ActiveItem>(); // Still fetching, but not used here
    }

    void OnEnable()
    {
        playerCamera = SceneCore.camera; // Assign player camera from SceneCore
        pcc = SceneCore.playerCharacter.GetComponent<PlayerCharacterController>();
        defaultFOV = playerCamera.fieldOfView; // Store initial default FOV
        targetFOV = defaultFOV; // Initialize targetFOV to default
    }

    void Update()
    {
        // Update targetFOV based on whether attack button is held
        if (pcc.attackDown)
        {
            targetFOV = zoomFOV;
            D.Log("Zooming in with telescope", this, LogManager.LogCategory.Item);
        }
        else
        {
            targetFOV = defaultFOV;
            D.Log("Zooming out with telescope", this, LogManager.LogCategory.Item);
        }

        // Smoothly lerp camera FOV toward the target, but snap if very close
        if (Mathf.Abs(playerCamera.fieldOfView - targetFOV) > 0.1f)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
        else
        {
            playerCamera.fieldOfView = targetFOV;
        }
    }

    public void ResetFOV()
    {
        targetFOV = defaultFOV;
        playerCamera.fieldOfView = defaultFOV;
        D.Log("Emergency FOV reset triggered", this, LogManager.LogCategory.Item);
    }
}
