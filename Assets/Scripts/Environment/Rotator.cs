using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class Rotator : MonoBehaviour
{
    
    [SerializeField] Vector3 angularSpeed;
    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(body.rotation.eulerAngles + Time.deltaTime * angularSpeed);
        body.MoveRotation(rotation);
    }
}
