using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float dashSpeed = 50f;
    [SerializeField, Min(0)] int stepsPerDash = 20; 
    int stepsDashing;
    PlayerController playerController;
    Rigidbody body;
    Transform orientation;
    Vector3 velocity, dashDirection;
    float initSpeed, currentSpeed;
    bool dashTried, dashing;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
    }

    void FixedUpdate()
    {
        velocity = body.velocity;
        currentSpeed = playerController.HorizontalSpeed;

        if (dashTried && !dashing) {
            initSpeed = currentSpeed;
            dashing = true;
            dashTried = false;
        }

        if (dashing) {
            stepsDashing += 1;
            if (currentSpeed < dashSpeed && !playerController.OnWall) {
                float speedDelta = dashSpeed - currentSpeed;
                velocity += speedDelta * dashDirection;
            }
            
            if (stepsDashing > stepsPerDash) {
                dashing = false;
                stepsDashing = 0;
                if (playerController.HorizontalSpeed > initSpeed) {
                    float speedDelta = playerController.HorizontalSpeed - initSpeed;
                    Vector3 currentDirection = new Vector3(velocity.x, 0, velocity.z).normalized;
                    velocity -= speedDelta * currentDirection;
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
