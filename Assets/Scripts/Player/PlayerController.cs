using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController: MonoBehaviour 
{
    [Header("Speeds/Accels")]
    [SerializeField, Range(0f, 100f)] float baseSpeed = 10f, climbSpeed = 3f;
    [SerializeField, Range(0f, 200f)] float baseAccel = 30f, airAccel = 10f, climbAccel = 12f, airSpeedGroundDecel = 50f;
    [SerializeField, Range(0f, 15f)] float jumpHeight = 2f, groundDrag = 10f;
    [SerializeField, Min(0)] int stepsForAirSpeed = 12, stepsTilJumpIgnored = 12;
    float flatSpeed, jumpSpeed;
    public float FlatSpeed => flatSpeed;
    bool jumpTried, climbTried;
    int stepsSinceLastGrounded, stepsSinceLastJump, stepsSinceJumpTried;

    [Header("Ground Snapping")]
    [SerializeField, Range(0f, 100f)] float maxSnapSpeed = 12f;
    [SerializeField, Min(0f)] float snapProbeDistance = 1f;
    [SerializeField] LayerMask snapProbeMask = -1, climbMask = -1;

    [Header("Angle Limits")]
    [SerializeField, Range(0, 90)] float maxGroundAngle = 45f, minWallJumpResetAngle = 90f;
    [SerializeField, Range(0, 180)] float maxclimbFacingAwayAngle = 90f;
    [SerializeField, Range(90, 170)] float maxClimbAngle = 140f;
    float minGroundDot, maxWallJumpDot, minClimbDot, minClimbFacingAwayDot;
    
    //Contact State
    Vector3 groundNormal, wallNormal, previousWallNormal, climbNormal;
    public Vector3 GroundNormal => groundNormal;
    public Vector3 WallNormal => wallNormal;
    int groundContactCount, wallContactCount, climbContactCount;
    public bool OnGround => groundContactCount > 0;
    public bool OnWall => wallContactCount > 0;
    public bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;

    Transform orientation;
    Rigidbody body;
    Vector3 desiredVelocity, inputDirection;

    Vector3 zAxis, xAxis, currentNormal;
    float currentSpeed, currentAccel;

    void Awake() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
        previousWallNormal = Vector3.zero;
        OnValidate();
    }

    void OnValidate() 
    {
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        maxWallJumpDot = Mathf.Cos(minWallJumpResetAngle * Mathf.Deg2Rad);

        minClimbDot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        minClimbFacingAwayDot = Mathf.Cos(maxclimbFacingAwayAngle * Mathf.Deg2Rad);

        jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
    }

    public void SetInputDirection(Vector3 direction) 
    {
        inputDirection = direction;
    }

    public void TryJump()
    {
        jumpTried = true;
        stepsSinceJumpTried = 0;
    }

    public void Climb(bool tried)
    {
        climbTried = tried;
    }

    public void PreventSnappingToGround() 
    {
        stepsSinceLastJump = -1;
    }

    void FixedUpdate() 
    {
        desiredVelocity = body.velocity;
        UpdateState();
        SetMovementAxis();
        SetSpeedAndAccel();
        EvaluateInputDirection();
        CustomDrag();
        LimitSpeed();

        if (jumpTried) {
            Jump();
        }
        if (Climbing) {
            desiredVelocity -= climbNormal * (climbAccel * Time.deltaTime * 0.9f);
            desiredVelocity += -Physics.gravity * Time.deltaTime;
        } 
        else if (OnGround && inputDirection == Vector3.zero) {
            desiredVelocity += -Physics.gravity * Time.deltaTime;
        }
        body.velocity = desiredVelocity;
        ClearState();
    }

    void UpdateState() 
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        stepsSinceJumpTried += 1;

        if (Climbing || OnGround || SnapToGround()) {
            stepsSinceLastGrounded = 0;
            previousWallNormal = Vector3.zero;
        }
        else {
            groundNormal = Vector3.up;
        }
    }
    
    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
            return false;

        float speed = desiredVelocity.magnitude;
        if (speed > maxSnapSpeed)
            return false;

        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, snapProbeDistance, snapProbeMask, QueryTriggerInteraction.Ignore))
            return false;
        
        if (hit.normal.y < minGroundDot)
            return false;

        groundContactCount = 1;
        groundNormal = hit.normal;
        float dot = Vector3.Dot(desiredVelocity, hit.normal);
        if (dot > 0) //if velocity points upward away from plane
            desiredVelocity = (desiredVelocity - hit.normal * dot).normalized * speed;

        return true;
    }

    void SetMovementAxis()
    {
        xAxis = orientation.right;
        zAxis = orientation.forward;
        currentNormal = groundNormal;

        if (Climbing) {
            xAxis = Vector3.Cross(climbNormal, Vector3.up);
            zAxis = Vector3.up;
            currentNormal = climbNormal;
        }

        xAxis = Vector3.ProjectOnPlane(xAxis, currentNormal).normalized;
        zAxis = Vector3.ProjectOnPlane(zAxis, currentNormal).normalized;
    }

    void SetSpeedAndAccel()
    {
        flatSpeed = Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude;

        if (Climbing) {
            currentSpeed = climbSpeed;
            currentAccel = climbAccel;
        }   
        else if (OnGround) {
            currentSpeed = baseSpeed;
            currentAccel = baseAccel;
        } 
        else {
            currentSpeed = baseSpeed;
            currentAccel = airAccel;
        }
    }

    void EvaluateInputDirection() 
    {
        /*Vector3 relativeVelocity = desiredVelocity - connectedVelocity;
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);
                    
        float speedChange = currentAccel * Time.deltaTime;
        float newX = Mathf.MoveTowards(currentX, inputDirection.x * currentSpeed, speedChange);
        float newZ = Mathf.MoveTowards(currentZ, inputDirection.z * currentSpeed, speedChange);

        desiredVelocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);*/

        float xDelta = inputDirection.x * currentAccel * Time.deltaTime;
        float zDelta = inputDirection.z * currentAccel * Time.deltaTime;
        
        desiredVelocity += xAxis * xDelta + zAxis * zDelta;
    }

    void CustomDrag()
    {
        if (OnGround && inputDirection == Vector3.zero) {
            body.drag = groundDrag;
        }
        else {
            body.drag = 0;
        }
    }

    void LimitSpeed() 
    {
        if (Climbing || OnGround) {
            if (desiredVelocity.sqrMagnitude > currentSpeed * currentSpeed) {
                desiredVelocity = desiredVelocity.normalized * currentSpeed;
            }
        }
        else {
            if (flatSpeed > currentSpeed) {
                Vector3 limitedVelocity = desiredVelocity.normalized * currentSpeed;
                desiredVelocity = new Vector3(limitedVelocity.x, desiredVelocity.y, limitedVelocity.z);
            }
        }   
    }

    void Jump() 
    {
        if (stepsSinceJumpTried > stepsTilJumpIgnored)
            jumpTried = false;

        Vector3 jumpDirection;
        if (OnGround) {
            jumpDirection = groundNormal;
        }
        else if (OnWall && Vector3.Dot(previousWallNormal, wallNormal) <= maxWallJumpDot) {
            jumpDirection = wallNormal;
            previousWallNormal = wallNormal;
        }
        else {
            return;
        }
        
        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float currentJumpSpeed = jumpSpeed;
        float alignedSpeed = Vector3.Dot(desiredVelocity, jumpDirection);
        if (alignedSpeed > 0f) {
            currentJumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        desiredVelocity += currentJumpSpeed * jumpDirection;

        stepsSinceLastJump = 0;
        jumpTried = false;
    }

    void ClearState() 
    {
        groundContactCount = wallContactCount = climbContactCount = 0;
        groundNormal = wallNormal = climbNormal = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision) 
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision) 
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision) 
    {
        for (int i = 0; i < collision.contactCount; i++) {
            int layer = collision.gameObject.layer;
            Vector3 normal = collision.GetContact(i).normal;

            //ground contact
            if (normal.y >= minGroundDot) {
                groundContactCount += 1;
                groundNormal += normal;
            } 
            else {
                //wall contact
                if (normal.y > -0.01f) {
                    wallContactCount += 1;
                    wallNormal += normal;
                }
                //climbing contact
                if (climbTried && 
                    normal.y >= minClimbDot && 
                    (climbMask & (1 << layer)) != 0 &&
                    FacingWall(orientation.forward, normal, minClimbFacingAwayDot)
                ){
                    climbContactCount += 1;
                    climbNormal += normal;
                }
            }
        }
        if (groundContactCount > 1) 
            groundNormal.Normalize();
        
        if (climbContactCount > 1)
            climbNormal.Normalize();

        if (wallContactCount > 1)
            wallNormal.Normalize();

    }

    public bool FacingWall(Vector3 facingDir, Vector3 wallNormal, float minAngleCosine)
    {
        return Vector3.Dot(facingDir, -wallNormal) >= minAngleCosine;
    }
}