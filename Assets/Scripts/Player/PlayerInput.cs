using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof (PlayerController))]
public class PlayerInput : MonoBehaviour
{
    InputController inputController;
    PlayerController playerController;
    Vector3 movementDirection;

    void Awake()
    {
        inputController = new InputController();
        playerController = GetComponent<PlayerController>();
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
    }

    void OnDisable()
    {
        inputController.Player.Disable();
        inputController.Player.Movement.performed -= OnMovementPerformed;
        inputController.Player.Movement.canceled -= OnMovementCanceled;
        inputController.Player.Climb.performed -= OnClimbPerformed;
        inputController.Player.Climb.canceled -= OnClimbCanceled;
        inputController.Player.Jump.performed -= OnJumpPerformed;
    }

    void OnMovementPerformed(InputAction.CallbackContext value)
    {
        movementDirection = Vector3.ClampMagnitude(value.ReadValue<Vector3>(), 1);        
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

    public Vector2 GetMouseDelta()
    {
        return inputController.Player.Look.ReadValue<Vector2>();
    }
}
