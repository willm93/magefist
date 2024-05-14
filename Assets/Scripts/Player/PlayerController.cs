using System;
using System.Collections;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController : MonoBehaviour 
{
    [SerializeField] MoveStateParams defaultMoveStateParams;
    [SerializeField, Range(0f, 25f)] float groundDrag = 10f;
    float currentSpeed, currentYSpeed;
    public float CurrentSpeed => currentSpeed; 
    public float CurrentYSpeed => currentYSpeed;
    public MoveState CurrentMoveState => currentMoveParams.state;
    int stepsSinceLastGrounded;

    [Header("Crouching")]
    [SerializeField, Range(0f,1f)] float crouchSpeedModifier;
    [SerializeField, Range(0f,1f)] float crouchYScale = 0.5f;
    float initYScale;
    bool crouching, uncrouchQueued;

    [Header("Jumping")]
    [SerializeField, Range(0f, 25f)] float jumpForce = 5f; 
    [SerializeField, Range(0f, 25f)] float wallJumpForce = 4f;
    [SerializeField, Min(0)] int stepsTilJumpIgnored = 12;
    bool jumpTried;
    int stepsSinceLastJump, stepsSinceJumpTried;

    [Header("Ground Snapping")]
    [SerializeField, Range(0f, 100f)] float maxSnapSpeed = 12f;
    [SerializeField, Min(0f)] float snapProbeDistance = 1f;
    [SerializeField] LayerMask snapProbeMask = -1;

    [Header("Angle Limits")]
    [SerializeField, Range(0f, 90f)] float maxGroundAngle = 45f;
    [SerializeField, Range(0f, 90f)] float minWallJumpResetAngle = 90f;
    float minGroundDot, maxWallJumpDot;
    public float MinGroundDot => minGroundDot;
    
    //Contact State
    Vector3 groundNormal, wallNormal, previousWallNormal;
    int groundContactCount, wallContactCount;
    public Vector3 GroundNormal => groundNormal;
    public bool OnGround => groundContactCount > 0 && stepsSinceLastJump > 2;
    public bool OnWall => wallContactCount > 0 && stepsSinceLastJump > 2;
    public bool JustJumped => stepsSinceLastJump < 2;

    MoveStateParams currentMoveParams, lastMoveStateParams, momentumStateParams;
    float desiredSpeed;
    bool moveStateChanged; 
    int momentumCount;
    public int MomentumCount => momentumCount;
    public Action<MoveState> OnStateChange;

    Transform orientation;
    Rigidbody body;
    Vector3 desiredVelocity, inputDirection;
    Vector3 zAxis, xAxis;
    float moveSpeed, inputAccel;

    void Awake() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");

        previousWallNormal = Vector3.zero;
        currentMoveParams = lastMoveStateParams = defaultMoveStateParams;
        desiredSpeed = currentMoveParams.speed;
        moveSpeed = desiredSpeed;
        initYScale = transform.localScale.y;

        OnValidate();
    }

    void OnValidate() 
    {
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        maxWallJumpDot = Mathf.Cos(minWallJumpResetAngle * Mathf.Deg2Rad);
    }

    public void SetInputDirection(Vector3 direction) 
    {
        inputDirection = direction;
    }

    public void ChangeMoveState(MoveStateParams stateParams) 
    {
        lastMoveStateParams = currentMoveParams;
        currentMoveParams = stateParams;
        moveStateChanged = lastMoveStateParams.state != currentMoveParams.state;

        if (crouching && !currentMoveParams.allowsCrouching)
            Uncrouch();

        OnStateChange?.Invoke(stateParams.state);
    }

    public void ResetMoveState() 
    {
        lastMoveStateParams = currentMoveParams;
        currentMoveParams = defaultMoveStateParams;
        moveStateChanged = lastMoveStateParams.state != currentMoveParams.state;

        if (crouching && !currentMoveParams.allowsCrouching)
            Uncrouch();

        OnStateChange?.Invoke(MoveState.Default);
    }

    public void TryJump()
    {
        jumpTried = true;
        stepsSinceJumpTried = 0;
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

        if (uncrouchQueued) {
            Uncrouch();
        }

        if (OnGround && inputDirection == Vector3.zero) {
            body.useGravity = false;
        }
        else {
            body.useGravity = currentMoveParams.hasGravity;
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

        if (currentMoveParams.resetsJumps || OnGround || SnapToGround()) {
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
        Vector3 currentNormal = groundNormal;
        
        if (currentMoveParams.setsAxis) {
            xAxis = Vector3.Cross(currentMoveParams.normal, Vector3.up);
            zAxis = Vector3.up;
            currentNormal = currentMoveParams.normal;
        }

        xAxis = Vector3.ProjectOnPlane(xAxis, currentNormal).normalized;
        zAxis = Vector3.ProjectOnPlane(zAxis, currentNormal).normalized;
    }

    void SetSpeedAndAccel()
    { 
        inputAccel = OnGround ? currentMoveParams.groundAccel : currentMoveParams.airAccel;

        if (moveStateChanged) {
            desiredSpeed = currentMoveParams.speed;
            if (lastMoveStateParams.hasMomentum && currentMoveParams.acceptsMomentum &&
                lastMoveStateParams.speed > currentMoveParams.speed
            ) {
                momentumCount += 1;
                momentumStateParams = lastMoveStateParams;
                StartCoroutine(SmoothDecel(
                    momentumStateParams.speed, 
                    desiredSpeed, 
                    momentumStateParams.groundDecel, 
                    momentumStateParams.airDecel
                ));
            }
            else {
                momentumCount = 0;
                StopAllCoroutines();
                moveSpeed = desiredSpeed;
            }
            moveStateChanged = false;
        }
    }

    IEnumerator SmoothDecel(float initSpeed, float targetSpeed, float groundDecel, float airDecel) 
    {
        float speedDifference = Mathf.Abs(targetSpeed - initSpeed);
        float speedRemoved = 0f;
        float deceleration;

        while (speedRemoved < speedDifference) {
            deceleration = OnGround ? groundDecel : airDecel;
            moveSpeed -= deceleration * Time.deltaTime;
            speedRemoved += deceleration * Time.deltaTime;
            yield return null;
        }

        if (momentumCount == 1)
            moveSpeed = targetSpeed;

        momentumCount -= 1;
    }

    void EvaluateInputDirection() 
    {
        if (currentMoveParams.blocksMoveInput)
            return;
        
        float xDelta = inputDirection.x * inputAccel * Time.deltaTime;
        float zDelta = inputDirection.z * inputAccel * Time.deltaTime;
        Vector3 inputVelocity = xAxis * xDelta + zAxis * zDelta;

        if (OnWall) {
            float intoWallDot = Vector3.Dot(inputVelocity.normalized, -wallNormal);
            inputVelocity *= Mathf.Clamp01(1 - intoWallDot);
        }
        
        desiredVelocity += inputVelocity;
    }

    void ApplyDrag()
    {
        if (OnGround && currentMoveParams.hasGroundDrag && inputDirection == Vector3.zero) {
            body.drag = groundDrag;
        }
        else if (currentMoveParams.hasUngroundedDrag) {
            body.drag = groundDrag;
        }
        else {
            body.drag = 0;
        }
    }

    void LimitVelocity() 
    {
        float speedLimit = (crouching && OnGround) ? moveSpeed * crouchSpeedModifier : moveSpeed;

        if (currentMoveParams.limitsAllVelocity || OnGround) {
            if (desiredVelocity.sqrMagnitude > speedLimit * speedLimit) {
                desiredVelocity = desiredVelocity.normalized * speedLimit;
            }
        }
        else if (momentumCount > 0) {
            if (currentSpeed > speedLimit) {
                desiredVelocity = LimitedXZVelocity(speedLimit);
            }
            if (currentYSpeed > momentumStateParams.ySpeed) {
                desiredVelocity.y = momentumStateParams.ySpeed;
            }
        }
        else {
            if (currentSpeed > speedLimit) {
                desiredVelocity = LimitedXZVelocity(speedLimit);
            }
        }
    }

    Vector3 LimitedXZVelocity(float speedLimit)
    {
        Vector3 limitedVelocity = desiredVelocity.normalized * speedLimit;
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

    public void Crouch() 
    {
        if (!crouching && currentMoveParams.allowsCrouching) {
            Vector3 scale = transform.localScale;
            scale.Set(transform.localScale.x, crouchYScale, transform.localScale.z);
            transform.localScale = scale;
            if (OnGround)
                body.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            crouching = true;
            uncrouchQueued = false;
        }
    }

    public void Uncrouch()
    {
        bool canUncrouch = CanUncrouch();
        if (crouching && canUncrouch) {
            Vector3 scale = transform.localScale;
            scale.Set(transform.localScale.x, initYScale, transform.localScale.z);
            transform.localScale = scale;
            crouching = false;
            uncrouchQueued = false;
        } 
        else if (crouching && !canUncrouch) {
            uncrouchQueued = true;
        }
    }

    bool CanUncrouch() 
    {
        float distance = 2 * initYScale - crouchYScale - transform.localScale.x * 0.5f;
        return !Physics.SphereCast(
            transform.position, 
            transform.localScale.x * 0.5f - 0.1f, 
            Vector3.up, 
            out RaycastHit _hit, 
            distance
        );   
    }

    void ClearContacts() 
    {
        groundContactCount = wallContactCount = 0;
        groundNormal = wallNormal = Vector3.zero;
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
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y >= minGroundDot) {
                groundContactCount += 1;
                groundNormal += normal;
            } 
            else if (normal.y > -0.01f) {
                wallContactCount += 1;
                wallNormal += normal;
            }
        }
        if (groundContactCount > 1) 
            groundNormal.Normalize();

        if (wallContactCount > 1)
            wallNormal.Normalize();
    }
}
