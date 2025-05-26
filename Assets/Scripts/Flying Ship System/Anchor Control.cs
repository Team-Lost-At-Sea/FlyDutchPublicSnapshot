using UnityEngine;

public class AnchorControl : MonoBehaviour
{
    [SerializeField] private Interactable interactTarget;
    [SerializeField] private SpringJoint anchorSpringJoint;
    [SerializeField] private float anchorMaxDropDistance = 30.0f;
    [SerializeField] private HingeJoint controlWheelHingeJoint;
    [SerializeField] private float controlWheelSpinFactor = 10.0f;
    [SerializeField] private Rigidbody shipPhysicsController;
    [SerializeField] private float shipDecelerationWhenAnchored = 0.05f;
    [SerializeField] private float shipStabilizationWhenAnchored = 120.0f;
    private bool extended = false;
    private Rigidbody anchorRbody;
    private CollisionReporter anchorCollisions;

    public bool anchored
    {
        get
        {
            return extended && anchorCollisions.collisions.Count > 0;
        }
    }

    private void OnEnable()
    {
        interactTarget.OnInteract += HandleInteract;
    }

    private void OnDisable()
    {
        interactTarget.OnInteract -= HandleInteract;
    }

    private void HandleInteract(GameObject whom)
    {
        if (extended) // Anchor is down, raises it
        {
            anchorSpringJoint.maxDistance = 0.0f;
            extended = false;
            interactTarget.actionTooltip = "Drop Anchor";
        }
        else // Anchor is up, drops it
        {
            anchorSpringJoint.maxDistance = anchorMaxDropDistance;
            extended = true;
            interactTarget.actionTooltip = "Raise Anchor";
            SceneCore.ship.physicsObject.KillMomentum(); // Stop the ship immediately when dropping the anchor
        }
    }

    private void Start()
    {
        anchorRbody = anchorSpringJoint.GetComponent<Rigidbody>();
        anchorCollisions = anchorSpringJoint.GetComponent<CollisionReporter>();
        HandleInteract(null); // Initialize anchor state
    }

    private void Update()
    {
        // spin control wheel according to vertical motion of anchor relative to ship
        var motor = controlWheelHingeJoint.motor;
        var velDiff = anchorRbody.linearVelocity - shipPhysicsController.linearVelocity;
        motor.targetVelocity = controlWheelSpinFactor *
            Vector3.Project(velDiff, transform.up).magnitude *
            Mathf.Sign(Vector3.Dot(velDiff, transform.up));
        controlWheelHingeJoint.motor = motor;
        // if the anchor is out and anchored, make the ship behave itself
        if (anchored)
        {
            shipPhysicsController.linearVelocity =
                Vector3.Lerp(
                    shipPhysicsController.linearVelocity,
                    Vector3.zero,
                    1.0f - Mathf.Pow(
                        1.0f - shipDecelerationWhenAnchored,
                        Time.deltaTime * 60.0f
                    )
                );
            shipPhysicsController.AddTorque(
                shipPhysicsController.mass * shipStabilizationWhenAnchored * Time.deltaTime *
                    Vector3.Cross(shipPhysicsController.transform.up, Vector3.up),
                ForceMode.Impulse
            );
        }
    }
    

}
