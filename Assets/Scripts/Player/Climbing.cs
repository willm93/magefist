using UnityEngine;

[RequireComponent(typeof (Rigidbody))]
public class Climbing : MonoBehaviour
{
    [SerializeField] MoveStateParams moveStateParams;
    [SerializeField, Range(0f, 180f)] float maxclimbFacingAwayAngle = 90f;
    [SerializeField, Range(90f, 170f)] float maxClimbAngle = 140f;
    [SerializeField] LayerMask climbMask = -1;
    float minClimbDot, minClimbFacingAwayDot;
    bool climbTried, moveStateNeedsReset;
    bool isClimbing;

    PlayerController pc;
    Transform orientation;
    Rigidbody body;
    

    void Awake()
    {
        minClimbDot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        minClimbFacingAwayDot = Mathf.Cos(maxclimbFacingAwayAngle * Mathf.Deg2Rad);

        pc = GetComponent<PlayerController>();
        pc.OnStateChange += OnStateChange;
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
    }
    
    public void Climb(bool tried)
    {
        climbTried = tried;
    }

    void FixedUpdate()
    {
        if (!moveStateNeedsReset && isClimbing) {
            pc.ChangeMoveState(moveStateParams);
            moveStateNeedsReset = true;
        }
        else if (moveStateNeedsReset && !isClimbing) {
            pc.ResetMoveState();
            moveStateNeedsReset = false;
        }

        if (isClimbing) {
            body.AddForce(-moveStateParams.normal * moveStateParams.groundAccel * 0.9f, ForceMode.Acceleration);
        }
    }

    void OnStateChange(MoveState state)
    {
        if (state != MoveState.Climbing) {
            isClimbing = false;
            moveStateNeedsReset = false;
        }
    }

    void OnCollisionEnter(Collision collision) 
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision) 
    {
        EvaluateCollision(collision);
    }

    void OnCollisionExit(Collision collision) 
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision) 
    {
        Vector3 collisionNormals = Vector3.zero;
        for (int i = 0; i < collision.contactCount && !pc.JustJumped; i++) {
            int layer = collision.gameObject.layer;
            Vector3 collisionNormal = collision.GetContact(i).normal;

            if (climbTried && 
                collisionNormal.y < pc.MinGroundDot &&
                collisionNormal.y >= minClimbDot &&
                IsClimbable(layer) &&
                FacingWall(orientation.forward, collisionNormal, minClimbFacingAwayDot)
            ){
                collisionNormals += collisionNormal;
            }
        }
        isClimbing = collisionNormals != Vector3.zero;
        moveStateParams.normal = collisionNormals.normalized;
    }

    bool IsClimbable(int layer)
    {
        return (climbMask & (1 << layer)) != 0;
    }

    bool FacingWall(Vector3 facingDir, Vector3 wallNormal, float minAngleCosine)
    {
        return Vector3.Dot(facingDir, -wallNormal) >= minAngleCosine;
    }
}
