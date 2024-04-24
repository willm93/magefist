using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof (PlayerController))]
[RequireComponent(typeof (AbilityController))]
public class PlayerInput : MonoBehaviour
{
    InputController inputController;
    PlayerController playerController;
    AbilityController abilityController;
    Vector3 movementDirection;

    void Awake()
    {
        inputController = new InputController();
        playerController = GetComponent<PlayerController>();
        abilityController = GetComponent<AbilityController>();
    }

    void Update()
    {
        playerController.SetInputDirection(movementDirection);
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
        playerController.Climb(true);
    }

    void OnClimbCanceled(InputAction.CallbackContext value)
    {
        playerController.Climb(false);
    }

    void OnJumpPerformed(InputAction.CallbackContext value)
    {
        playerController.TryJump();
    }

    void OnDashPerformed(InputAction.CallbackContext value)
    {
        abilityController.Dash(movementDirection);
    }

    public Vector2 GetMouseDelta()
    {
        return inputController.Player.Look.ReadValue<Vector2>();
    }
}
