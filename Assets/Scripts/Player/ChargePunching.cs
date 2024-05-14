using UnityEngine;

[RequireComponent(typeof (Rigidbody))]
public class ChargePunching : MonoBehaviour
{
    [SerializeField] MoveStateParams chargingStateParams;
    [SerializeField] MoveStateParams punchingStateParams;
    [SerializeField, Range(0f, 100f)] float punchForce = 50f, punchCoolDown = 2f, punchDuration = 0.5f, chargeDuration = 1.5f;
    float punchCDTimer, chargePercent;
    public float ChargePercent => chargePercent;
    public bool PunchOffCooldown => punchCDTimer <= 0;
    bool moveStateNeedsReset, charging;
    Vector3 punchDirection;

    PlayerController pc;
    Rigidbody body;
    Transform orientation;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        pc.OnStateChange += OnStateChange;
        body = GetComponent<Rigidbody>();
        orientation = transform.Find("Orientation");
    }

    void Update()
    {
        if (punchCDTimer > 0f)
            punchCDTimer -= Time.deltaTime;

        if (charging && chargePercent < 1f)
            chargePercent += Time.deltaTime / chargeDuration;
        
        else if (charging && chargePercent >= 1f) 
            EndCharge(false);
        
    }

    void OnStateChange(MoveState state)
    {
        if (state != MoveState.Punching && state != MoveState.Charging) {
            moveStateNeedsReset = false;
            EndCharge(true);
        }
    }

    public void StartCharge()
    {
        if (punchCDTimer > 0f)
            return;
            
        charging = true;
        pc.ChangeMoveState(chargingStateParams);
        moveStateNeedsReset = true;
    }

    public void EndCharge(bool canceled)
    {
        if (!charging) 
            return;

        charging = false;
        punchCDTimer = punchCoolDown;
        
        if (!canceled) 
            Punch();
        else if (moveStateNeedsReset) 
            pc.ResetMoveState();
        
        chargePercent = 0;
    }

    void Punch()
    {
        punchDirection = Vector3.ProjectOnPlane(orientation.forward, pc.GroundNormal);
        body.velocity = Vector3.zero;
        pc.ChangeMoveState(punchingStateParams);
        moveStateNeedsReset = true;
        body.AddForce(punchForce * punchDirection, ForceMode.Impulse);
        
        Invoke(nameof(ResetPunch), punchDuration);
    }

    void ResetPunch()
    {
        if (moveStateNeedsReset)
            pc.ResetMoveState();
    }
}
