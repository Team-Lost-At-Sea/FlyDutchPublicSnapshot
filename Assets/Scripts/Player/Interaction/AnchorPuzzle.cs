using UnityEngine;

public class AnchorPuzzle : MonoBehaviour
{
    [SerializeField] private Interactable interactTarget;
    [SerializeField] private HingeJoint controlWheelHingeJoint;
    [SerializeField] private float controlWheelSpinAmount = 360.0f; // degrees to spin per activation
    [SerializeField] private float spinSpeed = 90.0f; // degrees per second

    private bool isSpinning = false;
    private float remainingSpin = 0f;

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
        if (!isSpinning)
        {
            isSpinning = true;
            remainingSpin = controlWheelSpinAmount;
        }
    }

    private void Update()
    {
        if (isSpinning && remainingSpin > 0f)
        {
            float spinThisFrame = spinSpeed * Time.deltaTime;
            if (spinThisFrame > remainingSpin)
                spinThisFrame = remainingSpin;

            var motor = controlWheelHingeJoint.motor;
            motor.targetVelocity = spinThisFrame;
            controlWheelHingeJoint.motor = motor;

            remainingSpin -= spinThisFrame;

            if (remainingSpin <= 0f)
            {
                isSpinning = false;
                motor.targetVelocity = 0f;
                controlWheelHingeJoint.motor = motor;

                // Optional: trigger puzzle progression here!
                Debug.Log("AnchorPuzzle spin complete! Check puzzle state.");
            }
        }
    }
}
