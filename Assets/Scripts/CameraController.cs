using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float mouseSens = 25f;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Transform playerOrientation;
    float xRotationDeg = 0f;
    float yRotationDeg = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 mouseDelta = mouseSens * Time.deltaTime * playerInput.GetMouseDelta();
        xRotationDeg -= mouseDelta.y;
        xRotationDeg = Mathf.Clamp(xRotationDeg, -88f, 88f);
        yRotationDeg += mouseDelta.x;

        transform.rotation = Quaternion.Euler(xRotationDeg, yRotationDeg, 0f);
        playerOrientation.rotation = Quaternion.Euler(0, yRotationDeg, 0);
    }
}
