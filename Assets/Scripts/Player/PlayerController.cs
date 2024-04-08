using UnityEngine;

public class PlayerController: MonoBehaviour 
{
    [SerializeField] Transform playerCam;
    [SerializeField] Transform orientation;
    [SerializeField, Range(0f, 100f)] float maxSpeed = 10f, maxClimbSpeed = 3f;
    [SerializeField, Range(0f, 100f)] float maxAccel = 30f, maxAirAccel = 10f, maxClimbAccel = 12f;
    [SerializeField, Range(0f, 10f)] float jumpHeight = 2f;
    [SerializeField, Range(0, 5)] int maxAirJumps = 0;
    [SerializeField] bool airJumpReset;
    float acceleration;
    bool jumpTried;
    bool climbTried;
    float jumpSpeed;
    int jumps;
    int stepsSinceLastGrounded, stepsSinceLastJump;

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

    Rigidbody body, connectedBody, previousConnectedBody;
    Vector3 velocity, inputDirection, connectedVelocity;
    Vector3 connectionWorldPos, connectionLocalPos;

    void OnValidate() 
    {
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairDot = Mathf.Cos(maxStairAngle * Mathf.Deg2Rad);
        maxWallJumpDot = Mathf.Cos(minWallJumpResetAngle * Mathf.Deg2Rad);
        minClimbDot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        minClimbFacingAwayDot = Mathf.Cos(maxclimbFacingAwayAngle * Mathf.Deg2Rad);
        jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
    }

    void Awake() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        body = GetComponent<Rigidbody>();
        previousWallNormal = Vector3.zero;
        OnValidate();
    }

    public void SetInputDirection(Vector3 direction) 
    {
        inputDirection = direction;
    }

    public void TryJump()
    {
        jumpTried = true;
    }

    public void Climb(bool tried)
    {
        climbTried = tried;
    }

    public float HorizontalSpeed()
    {
        Vector2 horzVelocity = body.velocity.z * Vector2.up + body.velocity.x * Vector2.right;
        return horzVelocity.magnitude;
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
            velocity -= climbNormal * (maxClimbAccel * Time.deltaTime * 0.9f);
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
        velocity = body.velocity;

        if (Climbing || OnGround || SnapToGround()) {
            stepsSinceLastGrounded = 0;
            //the first physics step after a jump still counts as grounded because OnCollision is called after FixedUpdate
            //so FixedUpdate uses collisions from the previous step, which makes the player count as grounded 1 step after a jump
            if (stepsSinceLastJump > 1) 
                jumps = 0;
                
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
        float speed;
        Vector3 xAxis = orientation.right;
        Vector3 zAxis = orientation.forward;
        Vector3 normal = groundNormal;
        bool modifyAccelByDirection = false;

        if (Climbing) {
            speed = maxClimbSpeed;
            acceleration = maxClimbAccel;
            xAxis = Vector3.Cross(climbNormal, Vector3.up);
            zAxis = Vector3.up;
            normal = climbNormal;
        }
        else if (OnGround) {
            speed = maxSpeed;
            acceleration = maxAccel;
            if (HorizontalSpeed() > maxSpeed + 0.1f && stepsSinceLastJump < 32)
                modifyAccelByDirection = true;
        } 
        else {
            speed = maxSpeed;
            acceleration = maxAirAccel;
            if (HorizontalSpeed() > maxSpeed + 0.1f)
                modifyAccelByDirection = true;
        }

        xAxis = ProjectOnPlane(xAxis, normal).normalized;
        zAxis = ProjectOnPlane(zAxis, normal).normalized;

        Vector3 relativeVelocity = velocity - connectedVelocity;
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);

        if (modifyAccelByDirection) {
            //prevents decelerating to maxSpeed if input direction matches velocity direction
            acceleration *= 1 - Mathf.Max(0f, Vector3.Dot(inputDirection, new Vector3(currentX, 0, currentZ).normalized));
        }
            
        float maxSpeedChange = acceleration * Time.deltaTime;
        float newX = Mathf.MoveTowards(currentX, inputDirection.x * speed, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, inputDirection.z * speed, maxSpeedChange);

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
        jumpTried = false;
        Vector3 jumpDirection;
        if (OnGround) {
            jumpDirection = groundNormal;
        }
        else if (OnWall && Vector3.Dot(previousWallNormal, wallNormal) <= maxWallJumpDot) {
            jumpDirection = wallNormal;
            previousWallNormal = wallNormal;
            jumps -= 1;
            if (airJumpReset)
                jumps = 0;
        }
        else if (maxAirJumps > 0 && jumps <= maxAirJumps) {
            if (jumps == 0) 
                jumps = 1;
            jumpDirection = groundNormal;
        }
        else {
            return;
        }
        
        jumpDirection = (jumpDirection + Vector3.up).normalized;
        stepsSinceLastJump = 0;
        jumps += 1;
        float currentJumpSpeed = jumpSpeed;
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f)
            currentJumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        
        velocity += currentJumpSpeed * jumpDirection;    
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

            if (normal.y >= minDot) {
                groundContactCount += 1;
                groundNormal += normal;
                connectedBody = collision.rigidbody;
            } 
            else { 
                if (normal.y > -0.01f) 
                {
                    wallContactCount += 1;
                    wallNormal += normal;
                    if (groundContactCount == 0)
                        connectedBody = collision.rigidbody;
                }
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