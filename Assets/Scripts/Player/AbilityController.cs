using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float dashSpeed = 50f;
    [SerializeField, Min(0)] int stepsPerDash = 20; 
    [SerializeField, Range(0, 180)] float maxFacingWallAngle = 70f;
    float minFacingWallDot;
    int stepsDashing;
    PlayerController pc;
    Rigidbody body;
    Transform orientation;
    Vector3 velocity, dashDirection;
    float initSpeed, currentSpeed;
    bool dashTried, dashing;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");

        minFacingWallDot = Mathf.Cos(maxFacingWallAngle * Mathf.Deg2Rad);
    }

    void FixedUpdate()
    {
        velocity = body.velocity;
        currentSpeed = pc.HorizontalSpeed;

        if (dashTried && !dashing) {
            initSpeed = currentSpeed;
            dashing = true;
            dashTried = false;
        }

        if (dashing) {
            stepsDashing += 1;
            bool facingWall = pc.FacingWall(orientation.forward, pc.WallNormal, minFacingWallDot);
            if (currentSpeed < dashSpeed && !facingWall) {
                float speedDelta = dashSpeed - currentSpeed;
                velocity += pc.ProjectOnPlane(speedDelta * dashDirection, pc.GroundNormal);
            }
            
            if (stepsDashing > stepsPerDash) {
                dashing = false;
                stepsDashing = 0;
                if (pc.HorizontalSpeed > initSpeed) {
                    float speedDelta = pc.HorizontalSpeed - initSpeed;
                    velocity -= pc.ProjectOnPlane(speedDelta * velocity.normalized, pc.GroundNormal);
                }
            }
        }

        body.velocity = velocity;
    }

    public void TryDash(Vector3 direction)
    {
        if (!dashing) {
            dashTried = true;
            dashDirection = orientation.forward * direction.z + orientation.right * direction.x;
        }
    }
}
