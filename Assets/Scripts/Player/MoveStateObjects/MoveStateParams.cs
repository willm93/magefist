using UnityEngine;

public enum MoveState { Default, Dashing, Climbing, Charging, Punching }

[CreateAssetMenu]
public class MoveStateParams : ScriptableObject
{
    public MoveState state;
    public float groundAccel, airAccel;
    public float speed, ySpeed, groundDecel, airDecel;
    public bool hasMomentum, acceptsMomentum, hasGravity, hasGroundDrag, hasUngroundedDrag, setsAxis; 
    public bool blocksMoveInput, resetsJumps, hasSeparateYSpeed, allowsCrouching;
    public Vector3 normal;
}
