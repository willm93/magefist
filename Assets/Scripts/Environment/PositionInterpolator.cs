using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
[RequireComponent (typeof (AutomaticSlider))]
public class PositionInterpolator : MonoBehaviour
{
    
    [SerializeField] Vector3 to, toEulers;
    Rigidbody body;
    Vector3 from, fromEulers;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        from = body.position;
        fromEulers = body.rotation.eulerAngles;
    }

    public void Interpolate(float t)
    {
        Vector3 position = Vector3.LerpUnclamped(from, to ,t);
        Quaternion rotation = Quaternion.LerpUnclamped(Quaternion.Euler(fromEulers), Quaternion.Euler(toEulers), t);
        body.Move(position, rotation);
    }
}
