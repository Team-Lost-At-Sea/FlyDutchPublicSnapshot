using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Needle.Console;
using System.Collections;

[SelectionBase] // Automatically select the parent when its child is selected
[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    private enum MovementMode
    {
        Normal,
        Ladder,
        Noclip
    }

    public Camera playerCamera;
    public Inventory inventoryComponent;
    public ActionSound SuperjumpSound;
    public DisableHUD disableAndOrToggleHUD;
    public ToggleMap mapToggler;

    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float climbSpeed = 6f;
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float superJumpPower = 20f;
    [SerializeField] private float gravity = 10f;
    [SerializeField] private float lookSens = 1f;
    private const float lookSpeedMult = 0.1F; // Constant multipler to make looking around feel natural at lookspeed = 1 (default sensitivity setting for new players);
    [SerializeField] private float lookXLimit = 90f;
    [SerializeField] private float defaultHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float sprintMultiplier = 2f; // How much the walkspeed is multiplied by while sprinting.
    [SerializeField] private float maxInteractDistance = 2f;
    [SerializeField] private float noclipSpeed = 10f;
    private float NOCLIP_SPRINT_MULTIPLIER = 24f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private InputSystem_Actions inputActions;
    private Vector2 movementInput;
    private Vector2 lookInput;
    private Action<InputAction.CallbackContext> onJumpPerformed;
    private Action<InputAction.CallbackContext> onJumpCancelled;
    private Action<InputAction.CallbackContext> onSuperJumpPerformed;
    private Action<InputAction.CallbackContext> onSuperJumpCancelled;
    private Action<InputAction.CallbackContext> onCrouchPerformed;
    private Action<InputAction.CallbackContext> onCrouchCancelled;
    private Action<InputAction.CallbackContext> onSprintPerformed;
    private Action<InputAction.CallbackContext> onSprintCancelled;
    private bool isJumping = false;
    private bool isChargingSuperJump = false;
    private float superJumpCharge = 0;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool wasGrounded = false;
    private bool justLanded = false;

    private MovementMode movementMode = MovementMode.Normal;
    private GameObject movementMedium = null; // Represents the gameObject that the player is using to access a special mode of movement (?) - Isaac
    [SerializeField] private float fastMovementThreshold = 30f; // Units per second; tune as needed
    private Vector3 previousPosition;
    private float ropeJumpCooldown = 0f;
    [SerializeField] private float ropeJumpCooldownDuration = 0.5f; // tweak this as needed
    public bool attackDown = false;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = InputModeManager.Instance.inputActions;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        // Store Delegates
        onJumpPerformed = ctx => isJumping = true;
        onJumpCancelled = ctx => isJumping = false;
        onSuperJumpPerformed = ctx => SuperJumpCharging();
        onSuperJumpCancelled = ctx => SuperJumpReleased();
        onCrouchPerformed = ctx => isCrouching = true;
        onCrouchCancelled = ctx => isCrouching = false;
        onSprintPerformed = ctx => isSprinting = true;
        onSprintCancelled = ctx => isSprinting = false;

        // Bind actions to methods
        inputActions.Player.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => movementInput = Vector2.zero;

        inputActions.Player.Look.performed +=
            ctx => lookInput = ctx.ReadValue<Vector2>() * LookSensPrefs.GetMultiplier(ctx);
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        enableJumping(true); // Eventually the goal is to replace all the other event listeners with something like this, but only when that needs comes.
        enableSuperJumping(true);

        enableCrouching(true);
        enableSprinting(true);

        inputActions.Player.Interact.performed += ctx => HandleInteractInput();
        inputActions.Player.Previous.performed += ctx => HandleItemPrevInput();
        inputActions.Player.Next.performed += ctx => HandleItemNextInput();
        inputActions.Player.Drop.performed += ctx => HandleDropInput();

        inputActions.Player.SwitchTo1.performed += ctx => HandleSwitchToSlotInput(0);
        inputActions.Player.SwitchTo2.performed += ctx => HandleSwitchToSlotInput(1);
        inputActions.Player.SwitchTo3.performed += ctx => HandleSwitchToSlotInput(2);
        inputActions.Player.SwitchTo4.performed += ctx => HandleSwitchToSlotInput(3);

        inputActions.Player.SwitchScroll.performed += ctx => HandleScrollInput(ctx);

        inputActions.Player.Attack.performed += ctx => HandleAttackInput();
        inputActions.Player.Attack.canceled += ctx => HandleAttackCanceled();

        inputActions.Player.Menu.canceled += ctx => HandleMenuInput();
        inputActions.Player.CustomAction1.performed += ctx => HandleToggleHUD();
        inputActions.Player.CustomAction2.performed += ctx => HandleToggleNOCLIP();

    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks or unwanted behavior
        inputActions.Player.Move.performed -= ctx => movementInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled -= ctx => movementInput = Vector2.zero;

        inputActions.Player.Look.performed -=
            ctx => lookInput = ctx.ReadValue<Vector2>() * LookSensPrefs.GetMultiplier(ctx);
        inputActions.Player.Look.canceled -= ctx => lookInput = Vector2.zero;

        enableJumping(false);
        enableSuperJumping(false);

        enableCrouching(false);
        enableSprinting(false);

        inputActions.Player.Interact.performed -= ctx => HandleInteractInput();
        inputActions.Player.Previous.performed -= ctx => HandleItemPrevInput();
        inputActions.Player.Next.performed -= ctx => HandleItemNextInput();
        inputActions.Player.Drop.performed -= ctx => HandleDropInput();

        inputActions.Player.SwitchTo1.performed -= ctx => HandleSwitchToSlotInput(0);
        inputActions.Player.SwitchTo2.performed -= ctx => HandleSwitchToSlotInput(1);
        inputActions.Player.SwitchTo3.performed -= ctx => HandleSwitchToSlotInput(2);
        inputActions.Player.SwitchTo4.performed -= ctx => HandleSwitchToSlotInput(3);

        inputActions.Player.SwitchScroll.performed -= ctx => HandleScrollInput(ctx);

        inputActions.Player.Attack.performed -= ctx => HandleAttackInput();
        inputActions.Player.Attack.canceled -= ctx => HandleAttackCanceled();

        inputActions.Player.Menu.canceled -= ctx => HandleMenuInput();
        inputActions.Player.CustomAction1.performed -= ctx => HandleToggleHUD();
        inputActions.Player.CustomAction2.performed -= ctx => HandleToggleNOCLIP();

        inputActions.Player.Disable();
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;
        
        if (ropeJumpCooldown > 0f)
        {
            ropeJumpCooldown -= Time.deltaTime;
        }

        float displacementSpeed = (currentPosition - previousPosition).magnitude / Time.deltaTime;
        //D.Log($"Displacement Speed: {displacementSpeed} units/s", gameObject, LogManager.LogCategory.Move);
        if (displacementSpeed > fastMovementThreshold)
        {
            D.Log($"Fast movement detected: {displacementSpeed} > {fastMovementThreshold}", gameObject, LogManager.LogCategory.Move);
            SceneCore.commands.InvokePlayerHighSpeedMove(GetComponent<Collider>());
        }

        if (movementMode == MovementMode.Normal)
        {
            UpdateNormal();
        }
        else if (movementMode == MovementMode.Ladder)
        {
            UpdateOnLadder();
        }
        else if (movementMode == MovementMode.Noclip)
        {
            UpdateNoClip();
        }

        previousPosition = currentPosition; // update after movement
    }



    void UpdateNormal()
    {

        MovePlayer();
        HandleSuperJumpCharge();

        if (InputModeManager.Instance?.inputMode == InputModeManager.InputMode.Player)
        {
            HandleCameraRotation();
        }

        justLanded = (
            characterController.isGrounded && !wasGrounded &&
            Mathf.Abs(characterController.velocity.y) >= 0.1f
        );
        wasGrounded = characterController.isGrounded;
    }


    void UpdateOnLadder()
    {
        MovePlayerOnLadder();
        if (InputModeManager.Instance?.inputMode == InputModeManager.InputMode.Player)
        {
            HandleCameraRotation();
        }
        justLanded = false;
        wasGrounded = true;
    }

    void UpdateNoClip()
    {
        // Camera (mouse) movement handling
        if (InputModeManager.Instance?.inputMode == InputModeManager.InputMode.Player)
        {
            HandleCameraRotation();
        }

        // Movement handling below
        float speed = isSprinting ? noclipSpeed * NOCLIP_SPRINT_MULTIPLIER : noclipSpeed; // Sprinting increases speed
        Vector3 move = new Vector3(movementInput.x, 0f, movementInput.y);

        // Handle vertical movement using Jump and Crouch inputs
        if (isJumping) move.y = 1f;
        if (isCrouching) move.y = -1f;

        // Convert to world space
        move = transform.TransformDirection(move) * speed;

        // Move the player
        transform.position += move * Time.deltaTime;
    }


    private void MovePlayer()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        float sprintSpeed = walkSpeed * sprintMultiplier;

        float currSpeedX = (isSprinting ? sprintSpeed : walkSpeed); // (result ? TRUE result : FALSE result)
        float currSpeedY = (isSprinting ? sprintSpeed : walkSpeed);

        // Replaces sprint/walk speed with crouch speed, as crouch takes precedence over sprinting.
        // Ignores crouch speed and remains with sprint/walk speed if not crouching.
        currSpeedX = movementInput.x * (isCrouching ? crouchSpeed : currSpeedX);
        currSpeedY = movementInput.y * (isCrouching ? crouchSpeed : currSpeedY);

        float movementDirectionY = moveDirection.y;
        moveDirection = (right * currSpeedX) + (forward * currSpeedY);

        if (isJumping && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.height = isCrouching ? crouchHeight : defaultHeight;
        CollisionFlags flags = characterController.Move(moveDirection * Time.deltaTime);


        if ((flags & CollisionFlags.Above) != 0 && moveDirection.y > 0)
        {
            moveDirection.y = 0f;
        }

    }

    private void MovePlayerOnLadder()
{
    if (movementMedium)
    {
        Vector3 movement = Time.deltaTime * movementMedium.transform.up * climbSpeed;
        bool isRope = !!movementMedium.GetComponent<Rope>();

        if (isRope && ropeJumpCooldown <= 0f)
        {
            movement *= movementInput.y;

            Vector3 toRope = movementMedium.transform.position - transform.position;
            Vector3 projected = Vector3.ProjectOnPlane(toRope, movementMedium.transform.up);

            float ropeOffset = 1.5f;

            Vector3 offset =
                movementMedium.transform.forward * ropeOffset * 0.5f +
                movementMedium.transform.right * ropeOffset * 0.5f;

            movement += projected + offset;
        }
        else if (!isRope)
        {
            movement *= Vector3.Dot(
                -(transform.forward * movementInput.y).normalized,
                movementMedium.transform.forward
            );
        }

        var collFlags = characterController.Move(movement);
        if ((collFlags & CollisionFlags.Below) != 0)
        {
            RestoreMovementMode();
        }
        else if (isJumping)
        {
            if (isRope)
            {
                characterController.Move(0.5f * transform.forward);
                ropeJumpCooldown = ropeJumpCooldownDuration;
            }
            else
            {
                characterController.Move(0.5f * movementMedium.transform.forward);
            }

            RestoreMovementMode();
            moveDirection.y = jumpPower;
        }
    }
}


    public void RestoreMovementMode()
    {
        movementMode = MovementMode.Normal;
        movementMedium = null;
    }

    public void AttachToLadder(GameObject ladder)
    {
        moveDirection = Vector3.zero;
        movementMode = MovementMode.Ladder;
        movementMedium = ladder;
    }

    public void DetachFromLadder()
    {
        if (movementMode == MovementMode.Ladder)
        {
            movementMode = MovementMode.Normal;
            movementMedium = null;
            characterController.Move(0.5f * Vector3.up);
        }
    }

    public bool AttachedTo(GameObject medium)
    {
        return movementMedium == medium;
    }

    public GameObject GetMovementMedium()
    {
        return movementMedium;
    }

    private void HandleCameraRotation()
    {
        float lookSpeed = lookSens * lookSpeedMult;
        rotationX += -lookInput.y * lookSens * lookSpeedMult;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, lookInput.x * lookSens * lookSpeedMult, 0);
    }

    private void HandleInteractInput()
    {
        //Debug.Log("HandleInteractInput()");
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance))
        {
            Interactable whom = hit.collider.GetComponent<Interactable>();
            if (whom)
            {
                whom.receiveInteract(this.gameObject);
            }
        }
    }

    private void HandleScrollInput(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();

        if (scrollValue > 0)
        {
            HandleItemPrevInput(); // Scroll up (Prev Item)
        }
        else if (scrollValue < 0)
        {
            HandleItemNextInput(); // Scroll down (Next Item)
        }
    }
    private void HandleItemPrevInput()
    {
        D.Log("Prev item pressed", null, "Inv");
        inventoryComponent.switchToPrev();
    }

    private void HandleItemNextInput()
    {
        D.Log("Next item pressed", null, "Inv");
        inventoryComponent.switchToNext();
    }

    private void HandleDropInput()
    {
        D.Log("Drop pressed", null, "Inv");
        inventoryComponent.dropItem();
    }

    private void HandleSwitchToSlotInput(int slotNum)
    {
        inventoryComponent.switchToSlot(slotNum);
    }

    private void HandleAttackInput()
    {
        attackDown = true;
        inventoryComponent.attackWithActiveItem();
    }

    private void HandleAttackCanceled()
    {
        attackDown = false;
    }

    private void HandleSuperJumpCharge()
    {
        if (isChargingSuperJump)
        {
            superJumpCharge += Time.deltaTime;
            if (superJumpCharge > 3f) // Cap charge at 3 seconds
            {
                SuperJumpReleased();
                return;
            }
            else if (superJumpCharge > 0.2f && !characterController.isGrounded) // Release charge if player falls of a ledge. Accounts for small delay where the player is airborne while in their crouching animation.
            {
                D.Log("Releasing Superjump due to player falling off!", gameObject, "Move");
                D.Log($"isGrounded: {characterController.isGrounded}", gameObject, "Move");
                SuperJumpReleased();
                return;
            }


        }
    }
    private void SuperJumpCharging()
    {
        if (!characterController.isGrounded && superJumpCharge == 0) return; // Don't start charging if player isn't grounded

        // Disable jumping and crouching while charging SuperJump
        inputActions.Player.Crouch.performed -= ctx => isCrouching = true;
        inputActions.Player.Crouch.canceled -= ctx => isCrouching = false;
        enableJumping(false);
        isCrouching = true;
        isChargingSuperJump = true;
        superJumpCharge += Time.deltaTime;

    }

    private void SuperJumpReleased()
    {
        if (superJumpCharge == 0) return; // Don't do anything if the player pressed & released the superjump button without the correct

        // Re-enable jumping and crouching while charging SuperJump
        inputActions.Player.Crouch.performed += ctx => isCrouching = true;
        inputActions.Player.Crouch.canceled += ctx => isCrouching = false;
        enableJumping(true);

        if (superJumpCharge > 3f) // Cap charge at 3 seconds
        {
            superJumpCharge = 3f;
        }

        float superJumpPowerMultiplier = 1f + (superJumpCharge * 0.2f); // Convert charge time to percent: 1s = 20%

        SuperjumpSound.PlaySingleRandom(); //play sound effect
        moveDirection.y = superJumpPower * superJumpPowerMultiplier;

        D.Log($"SuperJumped! Charge time: {superJumpCharge}", gameObject, "Move");
        isCrouching = false;
        isChargingSuperJump = false;
        superJumpCharge = 0;

        Metrics.Set("super jumps", Metrics.Get("super jumps") + 1);
    }

    private void HandleMenuInput()
    {
        SceneCore.MainMenu();
    }

    // Event Listener Enablers (and Disablers)
    public void enableJumping(bool active)
    {
        if (active)
        {
            enableJumping(false); // unsub first to avoid duplicate event listeners.

            inputActions.Player.Jump.performed += onJumpPerformed;
            inputActions.Player.Jump.canceled += onJumpCancelled;
        }
        else
        {
            inputActions.Player.Jump.performed -= onJumpPerformed;
            inputActions.Player.Jump.canceled -= onJumpCancelled;
            isJumping = false;
        }
    }

    public void enableSuperJumping(bool active)
    {
        if (active)
        {
            enableSuperJumping(false); // unsub first to avoid duplicate event listeners.

            inputActions.Player.SuperJump.performed += onSuperJumpPerformed;
            inputActions.Player.SuperJump.canceled += onSuperJumpCancelled;
        }
        else
        {
            inputActions.Player.SuperJump.performed -= onSuperJumpPerformed;
            inputActions.Player.SuperJump.canceled -= onSuperJumpCancelled;
            isChargingSuperJump = false;
        }
    }

    public void enableCrouching(bool active)
    {
        if (active)
        {
            enableCrouching(false); // unsub first to avoid duplicate event listeners.

            inputActions.Player.Crouch.performed += onCrouchPerformed;
            inputActions.Player.Crouch.canceled += onCrouchCancelled;
        }
        else
        {
            inputActions.Player.Crouch.performed -= onCrouchPerformed;
            inputActions.Player.Crouch.canceled -= onCrouchCancelled;
            isCrouching = false;
        }
    }

    public void enableSprinting(bool active)
    {
        if (active)
        {
            enableSprinting(false); // unsub first to avoid duplicate event listeners.

            inputActions.Player.Sprint.performed += onSprintPerformed;
            inputActions.Player.Sprint.canceled += onSprintCancelled;
        }
        else
        {
            inputActions.Player.Sprint.performed -= onSprintPerformed;
            inputActions.Player.Sprint.canceled -= onSprintCancelled;
            isSprinting = false;
        }
    }

    private void HandleToggleHUD()
    {
        disableAndOrToggleHUD.ToggleHUD();
    }
    private void HandleToggleNOCLIP()
    {
        if (movementMode == MovementMode.Noclip)
        {
            movementMode = MovementMode.Normal;
            characterController.enabled = true; // Re-enable collisions
        }
        else
        {
            movementMode = MovementMode.Noclip;
            characterController.enabled = false; // Disable collisions
        }
    }

    // Getters & Setters

    public float getMaxInteractDistance()
    {
        return maxInteractDistance;
    }

    public bool AnyMovementInput()
    {
        return movementInput != Vector2.zero;
    }

    public bool JustLanded()
    {
        return justLanded;
    }

    public bool Climbing()
    {
        return movementMode == MovementMode.Ladder;
    }

    public float GetWalkSpeed()
    {
        return walkSpeed;
    }
    public void SetWalkSpeed(float newWalkSpeed)
    {
        walkSpeed = newWalkSpeed;
    }
}
