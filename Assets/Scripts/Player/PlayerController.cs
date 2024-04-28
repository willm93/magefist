using System.Collections;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController : MonoBehaviour 
{
    [Header("Speeds/Accels")]
    [SerializeField, Range(0f, 100f)] float defaultSpeed = 11f;
    [SerializeField, Range(0f, 100f)] float climbSpeed = 3f;
    [SerializeField, Range(0f, 200f)] float groundAccel = 30f, airAccel = 10f, climbAccel = 12f;
    [SerializeField, Range(0f, 25f)] float groundDrag = 10f;
    float currentSpeed, currentYSpeed;
    public float CurrentSpeed => currentSpeed; 
    public float CurrentYSpeed => currentYSpeed;
    bool climbTried;
    int stepsSinceLastGrounded, stepsSinceLastJump, stepsSinceJumpTried;

    [Header("Jumping")]
    [SerializeField, Range(0f, 25f)] float jumpForce = 5f, wallJumpForce = 4f;
    [SerializeField, Min(0)] int stepsTilJumpIgnored = 12;
    bool jumpTried;

    [Header("Ground Snapping")]
    [SerializeField, Range(0f, 100f)] float maxSnapSpeed = 12f;
    [SerializeField, Min(0f)] float snapProbeDistance = 1f;
    [SerializeField] LayerMask snapProbeMask = -1, climbMask = -1;

    [Header("Angle Limits")]
    [SerializeField, Range(0, 90)] float maxGroundAngle = 45f;
    [SerializeField, Range(0, 90)] float minWallJumpResetAngle = 90f;
    [SerializeField, Range(0, 180)] float maxclimbFacingAwayAngle = 90f;
    [SerializeField, Range(90, 170)] float maxClimbAngle = 140f;
    float minGroundDot, maxWallJumpDot, minClimbDot, minClimbFacingAwayDot;
    
    //Contact State
    Vector3 groundNormal, wallNormal, previousWallNormal, climbNormal;
    int groundContactCount, wallContactCount, climbContactCount;
    public Vector3 GroundNormal => groundNormal;
    public bool OnGround => groundContactCount > 0 && stepsSinceLastJump > 2;
    public bool OnWall => wallContactCount > 0 && stepsSinceLastJump > 2;
    public bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2;

    public enum MovementState { Default, Dashing }
    MovementState currentMoveState = MovementState.Default, lastMoveState = MovementState.Default;
    float desiredSpeed, lastDesiredSpeed, speedChangeFactor;
    float abilitySpeed, abilityYSpeed, abilitySpeedChangeFactor;
    bool keepMomentum;

    Transform orientation;
    Rigidbody body;
    Vector3 desiredVelocity, inputDirection;
    Vector3 zAxis, xAxis, currentNormal;
    float moveSpeed, inputAccel;

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
    }

    public void SetInputDirection(Vector3 direction) 
    {
        inputDirection = direction;
    }

    public void ChangeMoveState(MoveStateParams stateParams) {
        currentMoveState = stateParams.state;
        abilitySpeed = stateParams.abilitySpeed;
        abilityYSpeed = stateParams.abilityYSpeed;
        abilitySpeedChangeFactor = stateParams.abilitySpeedChangeFactor;
    }

    public void ResetMoveState() {
        currentMoveState = MovementState.Default;
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
        currentSpeed = Vector3.ProjectOnPlane(desiredVelocity, Vector3.up).magnitude;
        currentYSpeed = desiredVelocity.y;

        UpdateState();
        SetMovementAxis();
        SetSpeedAndAccel();
        EvaluateInputDirection();
        ApplyDrag();

        if (jumpTried) {
            Jump();
        }
        if (Climbing) {
            desiredVelocity += -climbNormal * (climbAccel * Time.deltaTime * 0.9f);
            body.useGravity = false;
        } 
        else if (OnGround && inputDirection == Vector3.zero) {
            body.useGravity = false;
        }
        else if (currentMoveState == MovementState.Default) {
            body.useGravity = true;
        }

        LimitVelocity();
        body.velocity = desiredVelocity;
        ClearContacts();
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

        if (!Physics.Raycast(
                body.position, Vector3.down, 
                out RaycastHit hit, snapProbeDistance,
                snapProbeMask, QueryTriggerInteraction.Ignore
                )
            )
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
        if (Climbing) {
            desiredSpeed = climbSpeed;
            inputAccel = climbAccel;
        }   
        else if (OnGround) {
            desiredSpeed = FindMoveStateSpeed();
            inputAccel = groundAccel;
        } 
        else {
            desiredSpeed = FindMoveStateSpeed();
            inputAccel = airAccel;
        }

        if (lastMoveState == MovementState.Dashing)
            keepMomentum = true;

        if (desiredSpeed != lastDesiredSpeed) {
            if (keepMomentum) {
                StopAllCoroutines();
                StartCoroutine(SmoothSpeedChange());
            }
            else {
                StopAllCoroutines();
                moveSpeed = desiredSpeed;
            }
        }
        lastDesiredSpeed = desiredSpeed;
        lastMoveState = currentMoveState;
    }

    float FindMoveStateSpeed() 
    {
        if (currentMoveState != MovementState.Default) {
            speedChangeFactor = abilitySpeedChangeFactor;
            return abilitySpeed;   
        }
        else {
            return defaultSpeed;
        }
    }

    IEnumerator SmoothSpeedChange() 
    {
        float time = 0;
        float speedDifference = Mathf.Abs(desiredSpeed - moveSpeed);
        float startValue = moveSpeed;
        float boostFactor = speedChangeFactor;

        while (time < speedDifference) {
            moveSpeed = Mathf.Lerp(startValue, desiredSpeed, time / speedDifference);
            time += Time.deltaTime * boostFactor;
            yield return null;
        }

        moveSpeed = desiredSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }

    void EvaluateInputDirection() 
    {
        if (currentMoveState == MovementState.Default) {
            float xDelta = inputDirection.x * inputAccel * Time.deltaTime;
            float zDelta = inputDirection.z * inputAccel * Time.deltaTime;
            Vector3 inputVelocity = xAxis * xDelta + zAxis * zDelta;

            if (OnWall) {
                float intoWallDot = Vector3.Dot(inputVelocity.normalized, -wallNormal);
                inputVelocity *= Mathf.Clamp01(1 - intoWallDot);
            }
            
            desiredVelocity += inputVelocity;
        }
    }

    void ApplyDrag()
    {
        if ((Climbing || OnGround) && currentMoveState == MovementState.Default && inputDirection == Vector3.zero) {
            body.drag = groundDrag;
        }
        else {
            body.drag = 0;
        }
    }

    void LimitVelocity() 
    {
        if (Climbing || OnGround) {
            if (desiredVelocity.sqrMagnitude > moveSpeed * moveSpeed) {
                desiredVelocity = desiredVelocity.normalized * moveSpeed;
            }
        }
        else if (keepMomentum) {
            if (currentSpeed > moveSpeed) {
                desiredVelocity = LimitedXZVelocity();
            }
            if (currentYSpeed > abilityYSpeed) {
                desiredVelocity.y = abilityYSpeed;
            }
        }
        else {
            if (currentSpeed > moveSpeed) {
                desiredVelocity = LimitedXZVelocity();
            }
        }   
    }

    Vector3 LimitedXZVelocity()
    {
        Vector3 limitedVelocity = desiredVelocity.normalized * moveSpeed;
        return new Vector3(limitedVelocity.x, desiredVelocity.y, limitedVelocity.z);
    }

    void Jump() 
    {
        if (stepsSinceJumpTried > stepsTilJumpIgnored)
            jumpTried = false;

        if (OnGround) {
            desiredVelocity.y = 0;
            body.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);

            stepsSinceLastJump = 0;
            jumpTried = false;
        }
        else if (OnWall && Vector3.Dot(previousWallNormal, wallNormal) <= maxWallJumpDot) {
            desiredVelocity.y = 0;
            previousWallNormal = wallNormal;
            Vector3 jumpDirection = (wallNormal + Vector3.up).normalized;
            body.AddForce(wallJumpForce * jumpDirection, ForceMode.Impulse);

            stepsSinceLastJump = 0;
            jumpTried = false;
        }
    }

    void ClearContacts() 
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

public struct MoveStateParams 
{
    public PlayerController.MovementState state;
    public float abilitySpeed, abilityYSpeed, abilitySpeedChangeFactor;

    public MoveStateParams(PlayerController.MovementState _state, float xzSpeed, float ySpeed, float speedChangeFactor) {
        state = _state;
        abilitySpeed = xzSpeed;
        abilityYSpeed = ySpeed;
        abilitySpeedChangeFactor = speedChangeFactor;
    }
}