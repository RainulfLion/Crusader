using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInput : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float mouseSensitivity = 3f;

    public Transform cameraPitch;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    public float jumpHeight = 1.25f;
    public float gravity = -20f;

    public bool autoDefendFromEnemies = true;
    public float autoDefendRadius = 2.5f;
    public LayerMask autoDefendEnemyLayers = ~0;
    public string autoDefendEnemyTag = "Enemy";
    public QueryTriggerInteraction autoDefendTriggerInteraction = QueryTriggerInteraction.Collide;
    public bool defending;

    private CharacterController _controller;
    private Health _health;
    private PlayerAnim _playerAnim;
    private CombatResolver _combatResolver;

    private Collider[] _enemyHits;

    private float _pitch;
    private float _verticalVelocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _health = GetComponentInParent<Health>();
        _playerAnim = GetComponentInChildren<PlayerAnim>();
        _combatResolver = GetComponentInParent<CombatResolver>();
        _enemyHits = new Collider[16];
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (_health != null && _health.isDead) return;

        HandleLook();
        HandleMoveAndJump();
        UpdateDefendFromEnemies();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        if (cameraPitch == null) return;

        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        cameraPitch.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleMoveAndJump()
    {
        if (_controller == null) return;

        bool grounded = _controller.isGrounded;
        if (grounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveWorld = transform.TransformDirection(input) * moveSpeed;

        if (grounded && Input.GetButtonDown("Jump"))
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = new Vector3(moveWorld.x, _verticalVelocity, moveWorld.z);
        _controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateDefendFromEnemies()
    {
        if (_playerAnim == null) return;

        bool shouldDefend = false;
        if (autoDefendFromEnemies && autoDefendRadius > 0f)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                autoDefendRadius,
                _enemyHits,
                autoDefendEnemyLayers,
                autoDefendTriggerInteraction);

            if (string.IsNullOrEmpty(autoDefendEnemyTag))
            {
                shouldDefend = count > 0;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Collider c = _enemyHits[i];
                    if (c == null) continue;
                    if (c.CompareTag(autoDefendEnemyTag) || c.transform.root.CompareTag(autoDefendEnemyTag))
                    {
                        shouldDefend = true;
                        break;
                    }
                }
            }
        }

        defending = shouldDefend;
        _playerAnim.defending = defending;

        if (_combatResolver != null)
            _combatResolver.SetDefending(defending);
    }
}
