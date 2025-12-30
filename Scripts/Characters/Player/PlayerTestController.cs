using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;  // <-- This line MUST exist

public class PlayerTestController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float mouseSensitivity = 3f;

    // How strong the mouse flick needs to be to change guard direction
    public float guardFlickThreshold = 0.5f;

    private CharacterController controller;
    private Animator anim;

    public int guardIndex = 1;

    private float currentGSX = 0f;
    private float currentGSY = 1f;     // default to High guard
    public float guardLerpSpeed = 10f; // how fast to blend between guards
    public bool defending;             // exposed so we can see it in the Inspector
    public bool guardSelectHeld;        // true only while RMB is held (used for selecting guard with WASD)
    public float slotComboWindow = 0.1f;
    private float lastLeftClickTime = -999f;
    private float lastRightClickTime = -999f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f;
    public float dashDoubleTapWindow = 0.25f;
    private bool isDashing;
    private Vector3 dashDirection;
    private float dashEndTime;
    private float lastTapW = -999f;
    private float lastTapA = -999f;
    private float lastTapS = -999f;
    private float lastTapD = -999f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>(); // or GetComponent<Animator>() if Animator is on same object
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
        HandleAttack();
        HandleFeint();
        HandleBlock();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMove()
    {
        HandleDashInput();

        if (isDashing)
        {
            controller.SimpleMove(dashDirection * dashSpeed);
            anim.SetFloat("Speed", 1f);
            if (Time.time >= dashEndTime)
                isDashing = false;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveWorld = transform.TransformDirection(input) * moveSpeed;

        controller.SimpleMove(moveWorld);

        // drive Idle/Run
        anim.SetFloat("Speed", input.magnitude);
    }

    void HandleDashInput()
    {
        float t = Time.time;

        if (defending) return;

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (t - lastTapW <= dashDoubleTapWindow)
                StartDash(transform.forward);
            lastTapW = t;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (t - lastTapS <= dashDoubleTapWindow)
                StartDash(-transform.forward);
            lastTapS = t;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (t - lastTapD <= dashDoubleTapWindow)
                StartDash(transform.right);
            lastTapD = t;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (t - lastTapA <= dashDoubleTapWindow)
                StartDash(-transform.right);
            lastTapA = t;
        }
    }

    void StartDash(Vector3 dir)
    {
        isDashing = true;
        dashDirection = dir.normalized;
        dashEndTime = Time.time + dashDuration;
    }

    void HandleAttack()
    {
        bool leftDown = Input.GetMouseButtonDown(0);
        if (!leftDown) return;

        TriggerAttackForGuard();
    }

    void HandleFeint()
    {
        bool mouse4Down = Input.GetMouseButtonDown(3) || (Mouse.current != null && Mouse.current.backButton.wasPressedThisFrame);
        if (!mouse4Down) return;

        TriggerFeintForGuard();
    }

    void TriggerAttackForGuard()
    {
        switch (guardIndex)
        {
            case 1: // High guard
                anim.SetTrigger("Slot");
                break;
            case 0: // Left guard
                anim.SetTrigger("SwingLR");
                break;
            case 2: // Right guard
                anim.SetTrigger("SwingRL");
                break;
        }
    }

    void TriggerFeintForGuard()
    {
        switch (guardIndex)
        {
            case 0: // Left guard
                anim.SetTrigger("FeintL");
                break;
            case 2: // Right guard
                anim.SetTrigger("FeintR");
                break;
            case 1: // High guard
                anim.SetTrigger("FeintH");
                break;
        }
    }

    void HandleBlock()
    {
        if (Mouse.current == null)
        {
            guardSelectHeld = false;
            return;
        }

        guardSelectHeld = Mouse.current.rightButton.isPressed;

        // Are we holding RMB?
        anim.SetBool("Defending", defending);

        if (guardSelectHeld)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                guardIndex = 1;
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                guardIndex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                guardIndex = 2;
            }
        }

        // Map guardIndex to TARGET blend tree coordinates
        float targetGSX = 0f;
        float targetGSY = 0f;

        switch (guardIndex)
        {
            case 0: // Left
                targetGSX = 0f; targetGSY = 1f; break;
            case 1: // High
                targetGSX = 1f; targetGSY = 0f; break;   
            case 2: // Right
                targetGSX = -1f; targetGSY = 0f; break;
        }

        // Smoothly move current values toward target
        currentGSX = Mathf.Lerp(currentGSX, targetGSX, Time.deltaTime * guardLerpSpeed);
        currentGSY = Mathf.Lerp(currentGSY, targetGSY, Time.deltaTime * guardLerpSpeed);

        // Feed the smoothed values into the animator
        anim.SetFloat("GSX", currentGSX);
        anim.SetFloat("GSY", currentGSY);  // <-- you were missing this semicolon

        // DEBUG
        float dbgX = anim.GetFloat("GSX");
        float dbgY = anim.GetFloat("GSY");
        UnityEngine.Debug.Log($"GuardIndex={guardIndex}  GSX={dbgX}  GSY={dbgY}  AnimObject={anim.gameObject.name}");
    }
}
