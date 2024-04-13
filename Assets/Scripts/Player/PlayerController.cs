using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController: MonoBehaviour 
{
    [SerializeField, Range(0f, 100f)] float baseSpeed = 10f, climbSpeed = 3f;
    [SerializeField, Range(0f, 200f)] float baseAccel = 30f, airAccel = 10f, climbAccel = 12f, airSpeedGroundDecel = 50f;
    [SerializeField, Range(0f, 10f)] float jumpHeight = 2f;
    [SerializeField, Min(0)] int stepsForAirSpeed = 12, stepsTilJumpIgnored = 12;
    float horizontalSpeed, airSpeed;
    public float HorizontalSpeed => horizontalSpeed;
    float jumpSpeed;
    bool jumpTried;
    bool climbTried;
    int stepsSinceLastGrounded, stepsSinceLastJump, stepsSinceJumpTried;

    [SerializeField, Range(0f, 100f)] float maxSnapSpeed = 12f;
    [SerializeField, Min(0f)] float snapProbeDistance = 1f;
    [SerializeField] LayerMask snapProbeMask = -1, stairsMask = -1, climbMask = -1;

    [SerializeField, Range(0, 90)] 
    float maxGroundAngle = 45f, maxStairAngle = 55f, minWallJumpResetAngle = 90f, maxclimbFacingAwayAngle = 80f;
    [SerializeField, Range(90, 170)] float maxClimbAngle = 140f;
    float minGroundDot, minStairDot, maxWallJumpDot, minClimbDot, minClimbFacingAwayDot;
    
    Vector3 groundNormal, wallNormal, previousWallNormal, climbNormal;
    int groundContactCount, wallContactCount, climbContactCount;
    public bool OnGround => groundContactCount > 0;
    public bool OnWall => wallContactCount > 0;
    public bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;

    Transform orientation;
    Rigidbody body, connectedBody, previousConnectedBody;
    Vector3 velocity, inputDirection, connectedVelocity;
    Vector3 connectionWorldPos, connectionLocalPos;

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
        minStairDot = Mathf.Cos(maxStairAngle * Mathf.Deg2Rad);
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
        EvaluateInputDirection();

        if (jumpTried) {
            Jump();
        }
        if (Climbing) {
            velocity -= climbNormal * (climbAccel * Time.deltaTime * 0.9f);
            velocity += -Physics.gravity * Time.deltaTime;
        } 
        else if (OnGround && velocity.sqrMagnitude < 0.01f){
            velocity += -Physics.gravity * Time.deltaTime;
        }
         
        body.velocity = velocity;
        ClearState();
    }

    void UpdateState() 
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        stepsSinceJumpTried += 1;
        velocity = body.velocity;

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
        }

        if (connectedBody) {
            if (connectedBody.isKinematic || connectedBody.mass >= body.mass)
                UpdateConnectionState();
        }
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

    void ClearState() 
    {
        groundContactCount = wallContactCount = climbContactCount = 0;
        groundNormal = wallNormal = climbNormal = Vector3.zero;
        
        previousConnectedBody = connectedBody;
        connectedBody = null;
        connectedVelocity = Vector3.zero;
    }

    void EvaluateInputDirection() 
    {
        float acceleration, speed;
        Vector3 xAxis = orientation.right;
        Vector3 zAxis = orientation.forward;
        Vector3 normal = groundNormal;

        if (Climbing) {
            xAxis = Vector3.Cross(climbNormal, Vector3.up);
            zAxis = Vector3.up;
            normal = climbNormal;
        }

        xAxis = ProjectOnPlane(xAxis, normal).normalized;
        zAxis = ProjectOnPlane(zAxis, normal).normalized;
        horizontalSpeed = ProjectOnPlane(velocity, normal).magnitude;

        if (Climbing) {
            speed = climbSpeed;
            acceleration = climbAccel;
        }   
        else if (OnGround) {
            if (airSpeed > baseSpeed) {
                airSpeed = Mathf.Max(airSpeed - airSpeedGroundDecel * Time.deltaTime, baseSpeed);
                if (horizontalSpeed < airSpeed) {
                    airSpeed = horizontalSpeed;
                }
                speed = airSpeed;
            } 
            else {
                speed = baseSpeed;
            }
            acceleration = baseAccel;
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
            speed = airSpeed;
            acceleration = airAccel;
        }

        Vector3 relativeVelocity = velocity - connectedVelocity;
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);
                    
        float speedChange = acceleration * Time.deltaTime;
        float newX = Mathf.MoveTowards(currentX, inputDirection.x * speed, speedChange);
        float newZ = Mathf.MoveTowards(currentZ, inputDirection.z * speed, speedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
            return false;

        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
            return false;

        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, snapProbeDistance, snapProbeMask, QueryTriggerInteraction.Ignore))
            return false;
        
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
            return false;

        groundContactCount = 1;
        groundNormal = hit.normal;
        connectedBody = hit.rigidbody;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0) //if velocity points upward away from plane
            velocity = (velocity - hit.normal * dot).normalized * speed;

        return true;
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
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f) {
            currentJumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        velocity += currentJumpSpeed * jumpDirection;

        stepsSinceLastJump = 0;
        jumpTried = false;
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
            float minDot = GetMinDot(layer);
            Vector3 normal = collision.GetContact(i).normal;

            //ground contact
            if (normal.y >= minDot) {
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
                    Vector3.Dot(orientation.forward, -normal) >= minClimbFacingAwayDot
                ){
                    climbContactCount += 1;
                    climbNormal += normal;
                    connectedBody = collision.rigidbody;
                }
            }
        }
    }

    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? minGroundDot : minStairDot;
    }

    Vector3 ProjectOnPlane(Vector3 vector, Vector3 normal) 
    {
        return vector - normal * Vector3.Dot(vector, normal);
    }
}