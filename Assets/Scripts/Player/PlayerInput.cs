using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof (PlayerController))]
[RequireComponent(typeof (Dashing))]
[RequireComponent(typeof (Climbing))]
public class PlayerInput : MonoBehaviour
{
    InputController inputController;
    PlayerController pc;
    Dashing dasher;
    Climbing climber;
    ChargePunching charger;
    Vector3 movementDirection;

    void Awake()
    {
        inputController = new InputController();
        pc = GetComponent<PlayerController>();
        dasher = GetComponent<Dashing>();
        climber = GetComponent<Climbing>();
        charger = GetComponent<ChargePunching>();
    }

    void Update()
    {
        pc.SetInputDirection(movementDirection);
    }

    void OnEnable()
    {
        inputController.Player.Enable();
        inputController.Player.Movement.performed += OnMovementPerformed;
        inputController.Player.Movement.canceled += OnMovementCanceled;
        inputController.Player.Climb.performed += OnClimbPerformed;
        inputController.Player.Climb.canceled += OnClimbCanceled;
        inputController.Player.Jump.performed += OnJumpPerformed;
        inputController.Player.Dash.performed += OnDashPerformed;
        inputController.Player.Crouch.performed += OnCrouchPerformed;
        inputController.Player.Crouch.canceled += OnCrouchCanceled;
        inputController.Player.Charge.performed += OnChargePerformed;
        inputController.Player.Charge.canceled += OnChargeCanceled;
    }

    void OnDisable()
    {
        inputController.Player.Disable();
        inputController.Player.Movement.performed -= OnMovementPerformed;
        inputController.Player.Movement.canceled -= OnMovementCanceled;
        inputController.Player.Climb.performed -= OnClimbPerformed;
        inputController.Player.Climb.canceled -= OnClimbCanceled;
        inputController.Player.Jump.performed -= OnJumpPerformed;
        inputController.Player.Dash.performed -= OnDashPerformed;
        inputController.Player.Crouch.performed -= OnCrouchPerformed;
        inputController.Player.Crouch.canceled -= OnCrouchCanceled;
        inputController.Player.Charge.performed -= OnChargePerformed;
        inputController.Player.Charge.canceled -= OnChargeCanceled;
    }

    void OnMovementPerformed(InputAction.CallbackContext value)
    {
        movementDirection = value.ReadValue<Vector3>();        
    }

    void OnMovementCanceled(InputAction.CallbackContext value)
    {
        movementDirection = Vector3.zero;
    }

    void OnClimbPerformed(InputAction.CallbackContext value)
    {
        climber.Climb(true);
    }

    void OnClimbCanceled(InputAction.CallbackContext value)
    {
        climber.Climb(false);
    }

    void OnJumpPerformed(InputAction.CallbackContext value)
    {
        pc.TryJump();
    }

    void OnDashPerformed(InputAction.CallbackContext value)
    {
        dasher.Dash(movementDirection);
    }

    void OnCrouchPerformed(InputAction.CallbackContext value)
    {
        pc.Crouch();
    }

    void OnCrouchCanceled(InputAction.CallbackContext value)
    {
        pc.Uncrouch();
    }

    void OnChargePerformed(InputAction.CallbackContext value)
    {
        charger.StartCharge();
    }

    void OnChargeCanceled(InputAction.CallbackContext value)
    {
        charger.EndCharge(true);
    }

    public Vector2 GetMouseDelta()
    {
        return inputController.Player.Look.ReadValue<Vector2>();
    }
}
