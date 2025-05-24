using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FastMovementZoneChecker : MonoBehaviour
{
    [SerializeField] private LayerMask zoneLayerMask; // Layer for trigger zones
    [SerializeField] private Collider playerCollider; // Assign your player's collider here (not the CharacterController itself)

    private CharacterController characterController;
    private Vector3 lastPosition;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        lastPosition = transform.position;

        if (playerCollider == null)
        {
            Debug.LogWarning("FastMovementZoneChecker: Please assign the playerCollider in the inspector.");
        }
    }

    void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        if (distance > 0f)
        {
            direction.Normalize();

            Vector3 bottom = lastPosition + characterController.center + Vector3.down * (characterController.height / 2f - characterController.radius);
            Vector3 top = lastPosition + characterController.center + Vector3.up * (characterController.height / 2f - characterController.radius);

            if (Physics.CapsuleCast(bottom, top, characterController.radius, direction, out RaycastHit hit, distance, zoneLayerMask))
            {
                var zone = hit.collider.GetComponent<TriggerZoneHandler>();
                if (zone != null)
                {
                    zone.CheckAndSetPlayerInside(playerCollider);
                }
            }
        }

        lastPosition = currentPosition;
    }
}
