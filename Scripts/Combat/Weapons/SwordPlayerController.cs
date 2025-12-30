using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;  // <-- This line MUST exist
#endif

[RequireComponent(typeof(CharacterController))]
public class SwordPlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float mouseSensitivity = 3f;

    private CharacterController controller;
    private Health _health;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        _health = GetComponentInParent<Health>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (_health != null && _health.isDead) return;

        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveWorld = transform.TransformDirection(input) * moveSpeed;

        controller.SimpleMove(moveWorld);
    }
}