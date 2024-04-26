using UnityEngine;

public class Dashing : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float dashForce = 50f, dashCoolDown = 2f, dashDuration = 0.5f;
    [SerializeField, Range(0f, 100f)] float dashSpeed = 30f, dashYSpeed = 10f, dashSpeedChangeFactor = 1f;
    float dashCDTimer;
    Vector3 dashDirection;
    MoveStateParams moveStateParams;

    PlayerController pc;
    Rigidbody body;
    Transform orientation;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
        PlayerController.MovementState state = PlayerController.MovementState.Dashing;
        moveStateParams = new MoveStateParams(state, dashSpeed, dashYSpeed, dashSpeedChangeFactor);
    }

    void Update()
    {
        if (dashCDTimer > 0)
            dashCDTimer -= Time.deltaTime;
    }

    public void Dash(Vector3 direction)
    {
        if (dashCDTimer > 0) 
            return;
        else 
            dashCDTimer = dashCoolDown;

        if (direction == Vector3.zero)
            dashDirection = orientation.forward;
        else
            dashDirection = orientation.forward * direction.z + orientation.right * direction.x;

        dashDirection = Vector3.ProjectOnPlane(dashDirection, pc.GroundNormal);

        body.velocity = Vector3.zero;
        body.useGravity = false;
        pc.ChangeMoveState(moveStateParams);
        body.AddForce(dashForce * dashDirection, ForceMode.Impulse);
        
        Invoke(nameof(ResetDash), dashDuration);
    }

    void ResetDash()
    {
        pc.ResetMoveState();
        body.useGravity = true;
    }
}
