using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController: MonoBehaviour 
{
    [Header("Speeds/Accels")]
    [SerializeField, Range(0f, 100f)] float baseSpeed = 10f, climbSpeed = 3f;
    [SerializeField, Range(0f, 200f)] float baseAccel = 30f, airAccel = 10f, climbAccel = 12f, airSpeedGroundDecel = 50f;
    [SerializeField, Range(0f, 10f)] float jumpHeight = 2f;
    [SerializeField, Min(0)] int stepsForAirSpeed = 12, stepsTilJumpIgnored = 12;
    float horizontalSpeed, airSpeed, jumpSpeed;
    public float HorizontalSpeed => horizontalSpeed;
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
    Rigidbody body, connectedBody, previousConnectedBody;
    Vector3 desiredVelocity, inputDirection, connectedVelocity;
    Vector3 connectionWorldPos, connectionLocalPos;

    Vector3 zAxis, xAxis, usedNormal;
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
        airSpeed = baseSpeed;
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
        UpdateState();
        SetAxis();
        SetSpeedAndAccel();
        EvaluateInputDirection();

        if (jumpTried) {
            Jump();
        }
        if (Climbing) {
            desiredVelocity -= climbNormal * (climbAccel * Time.deltaTime * 0.9f);
            desiredVelocity += -Physics.gravity * Time.deltaTime;
        } 
        else if (OnGround && desiredVelocity.sqrMagnitude < 0.01f) {
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
        desiredVelocity = body.velocity;

        if (Climbing || OnGround || SnapToGround()) {
            stepsSinceLastGrounded = 0;
                
            if (groundContactCount > 1) 
                groundNormal.Normalize();
            
            if (climbContactCount > 1)
                climbNormal.Normalize();

            previousWallNormal = Vector3.zero;
        }
        else {
            if (wallContactCount > 1)
                wallNormal.Normalize();

            groundNormal = Vector3.up;
        }

        if (connectedBody) {
            if (connectedBody.isKinematic || connectedBody.mass >= body.mass)
                UpdateConnectionState();
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
        connectedBody = hit.rigidbody;
        float dot = Vector3.Dot(desiredVelocity, hit.normal);
        if (dot > 0) //if velocity points upward away from plane
            desiredVelocity = (desiredVelocity - hit.normal * dot).normalized * speed;

        return true;
    }

    void UpdateConnectionState()
    {
        if (connectedBody == previousConnectedBody) {
            Vector3 connectedDisplacement = connectedBody.transform.TransformPoint(connectionLocalPos) - connectionWorldPos;
            connectedVelocity = connectedDisplacement / Time.deltaTime;
        }
        connectionWorldPos = body.position;
        connectionLocalPos = connectedBody.transform.InverseTransformPoint(connectionWorldPos);
    }

    void SetAxis()
    {
        xAxis = orientation.right;
        zAxis = orientation.forward;
        usedNormal = groundNormal;

        if (Climbing) {
            xAxis = Vector3.Cross(climbNormal, Vector3.up);
            zAxis = Vector3.up;
            usedNormal = climbNormal;
        }

        xAxis = ProjectOnPlane(xAxis, usedNormal).normalized;
        zAxis = ProjectOnPlane(zAxis, usedNormal).normalized;
    }

    void SetSpeedAndAccel()
    {
        horizontalSpeed = ProjectOnPlane(body.velocity, usedNormal).magnitude;

        if (Climbing) {
            currentSpeed = climbSpeed;
            currentAccel = climbAccel;
        }   
        else if (OnGround) {
            if (airSpeed > baseSpeed) {
                airSpeed = Mathf.Max(airSpeed - airSpeedGroundDecel * Time.deltaTime, baseSpeed);
                if (horizontalSpeed < airSpeed) {
                    airSpeed = horizontalSpeed;
                }
                currentSpeed = airSpeed;
            } 
            else {
                currentSpeed = baseSpeed;
            }
            currentAccel = baseAccel;
        } 
        else {
            if (horizontalSpeed > airSpeed && stepsSinceLastJump < stepsForAirSpeed) {
                airSpeed = horizontalSpeed;
            }
            else if (horizontalSpeed < baseSpeed) {
                airSpeed = baseSpeed;
            }
            else if (horizontalSpeed < airSpeed) {
                airSpeed = horizontalSpeed;
            }
            currentSpeed = airSpeed;
            currentAccel = airAccel;
        }
    }

    void EvaluateInputDirection() 
    {
        Vector3 relativeVelocity = desiredVelocity - connectedVelocity;
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);
                    
        float speedChange = currentAccel * Time.deltaTime;
        float newX = Mathf.MoveTowards(currentX, inputDirection.x * currentSpeed, speedChange);
        float newZ = Mathf.MoveTowards(currentZ, inputDirection.z * currentSpeed, speedChange);

        desiredVelocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
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
        
        previousConnectedBody = connectedBody;
        connectedBody = null;
        connectedVelocity = Vector3.zero;
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
                connectedBody = collision.rigidbody;
            } 
            else {
                //wall contact
                if (normal.y > -0.01f) {
                    wallContactCount += 1;
                    wallNormal += normal;
                    if (groundContactCount == 0)
                        connectedBody = collision.rigidbody;
                }
                //climbing contact
                if (climbTried && 
                    normal.y >= minClimbDot && 
                    (climbMask & (1 << layer)) != 0 &&
                    FacingWall(orientation.forward, normal, minClimbFacingAwayDot)
                ){
                    climbContactCount += 1;
                    climbNormal += normal;
                    connectedBody = collision.rigidbody;
                }
            }
        }
    }

    public Vector3 ProjectOnPlane(Vector3 vector, Vector3 normal) 
    {
        return vector - normal * Vector3.Dot(vector, normal);
    }

    public bool FacingWall(Vector3 facingDir, Vector3 wallNormal, float minAngleCosine)
    {
        return Vector3.Dot(facingDir, -wallNormal) >= minAngleCosine;
    }
}