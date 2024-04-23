using UnityEngine;

public class AbilityController : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float dashForce = 50f;
    PlayerController pc;
    Rigidbody body;
    Transform orientation;
    Vector3 dashDirection;
    bool dashing;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
    }

    public void TryDash(Vector3 direction)
    {
        /*if (!dashing) {
            dashing = true;
            if (direction == Vector3.zero) {
                dashDirection = orientation.forward;
            }
            else {
                dashDirection = orientation.forward * direction.z + orientation.right * direction.x;
            }
            dashDirection = Vector3.ProjectOnPlane(dashDirection, pc.GroundNormal);
            pc.SetSpeedLimit(50f);
            body.AddForce(dashForce * dashDirection, ForceMode.Impulse);

            Invoke(nameof(ResetDash), 1f);
        }*/
    }

    void ResetDash()
    {
        dashing = false;
    }
}
