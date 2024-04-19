using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float dashSpeed = 50f;
    [SerializeField, Min(0)] int stepsPerDash = 20;
    int stepsDashing;
    PlayerController pc;
    Rigidbody body;
    Transform orientation;
    Vector3 desiredVelocity, dashDirection;
    float initSpeed, currentSpeed;
    bool dashTried, dashing;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
    }

    void FixedUpdate()
    {
        desiredVelocity = body.velocity;
        currentSpeed = pc.HorizontalSpeed;

        if (dashTried && !dashing) {
            initSpeed = currentSpeed;
            dashing = true;
            dashTried = false;
        }

        if (dashing) {
            stepsDashing += 1;
            float intoWallDot = Vector3.Dot(dashDirection, -pc.WallNormal);
            if (currentSpeed < dashSpeed && stepsDashing <= stepsPerDash) {
                float speedDelta = dashSpeed - currentSpeed;
                speedDelta *= Mathf.Clamp01(1 - intoWallDot);
                desiredVelocity += pc.ProjectOnPlane(speedDelta * dashDirection, pc.GroundNormal);
            }

            desiredVelocity.y *= Mathf.Clamp01(1 - 0.18f * intoWallDot);

            if (stepsDashing > stepsPerDash) {
                dashing = false;
                stepsDashing = 0;
                if (pc.HorizontalSpeed > initSpeed) {
                    float speedDelta = pc.HorizontalSpeed - initSpeed;
                    desiredVelocity -= pc.ProjectOnPlane(speedDelta * desiredVelocity.normalized, pc.GroundNormal);
                    desiredVelocity.y = 0; //stops wierd super high bounces
                }
            }
        }

        body.velocity = desiredVelocity;
    }

    public void TryDash(Vector3 direction)
    {
        if (!dashing) {
            dashTried = true;
            if (direction == Vector3.zero) {
                dashDirection = orientation.forward;
            }
            else {
                dashDirection = orientation.forward * direction.z + orientation.right * direction.x;
            }
        }
    }
}
