using UnityEngine;

public enum MoveState { Default, Dashing, Climbing }

[CreateAssetMenu]
public class MoveStateParams : ScriptableObject
{
    public MoveState state;
    public float groundAccel, airAccel;
    public float speed, ySpeed, speedChangeFactor;
    public bool hasMomentum, acceptsMomentum, hasGravity, hasUngroundedDrag, setsAxis; 
    public bool blocksMoveInput, resetsJumps, limitsAllVelocity, allowsCrouching;
    public Vector3 normal;
}
