using UnityEngine;

public class PlayerAnim : MonoBehaviour
{
    public enum Guard
    {
        Left,
        High,
        Right,
    }

    [Header("References")]
    public Animator animator;
    public PlayerCombat playerCombat;

    [Header("Inputs")]
    public float guardFlickThreshold = 0.5f;
    public float guardLerpSpeed = 10f;

    [Header("Animator Params")]
    public string gsxParam = "GSX";
    public string gsyParam = "GSY";
    public string defendingBoolParam = "Defending";
    public string slotTrigger = "Slot";
    public string swingLRTrigger = "SwingLR";
    public string swingRLTrigger = "SwingRL";

    [Header("State")]
    public Guard currentGuard = Guard.High;
    public bool defending;

    private Vector2 _guardFlickAccum;
    private float _currentGSX;
    private float _currentGSY;
    private float _nextGuardSwitchTime;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (playerCombat == null) playerCombat = GetComponentInParent<PlayerCombat>();
        _guardFlickAccum = Vector2.zero;
        _nextGuardSwitchTime = 0f;
        ApplyGuardImmediate(currentGuard);
        SyncCombatGuard(currentGuard);
    }

    void Update()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(defendingBoolParam))
            animator.SetBool(defendingBoolParam, defending);

        HandleGuardSelection();
        UpdateGuardBlend();
        HandleAttacks();
    }

    private void HandleGuardSelection()
    {
        bool held = IsMouseButton4Held();
        if (!held)
        {
            _guardFlickAccum = Vector2.zero;
            return;
        }

        float dx = Input.GetAxisRaw("Mouse X");
        float dy = Input.GetAxisRaw("Mouse Y");
        _guardFlickAccum += new Vector2(dx, dy);

        if (Time.time < _nextGuardSwitchTime) return;
        if (_guardFlickAccum.sqrMagnitude < guardFlickThreshold * guardFlickThreshold) return;

        Guard next = currentGuard;
        float ax = Mathf.Abs(_guardFlickAccum.x);
        float ay = Mathf.Abs(_guardFlickAccum.y);

        if (ax >= ay)
            next = _guardFlickAccum.x >= 0f ? Guard.Right : Guard.Left;
        else
            next = _guardFlickAccum.y >= 0f ? Guard.High : Guard.High;

        SetGuard(next);
        _guardFlickAccum = Vector2.zero;
        _nextGuardSwitchTime = Time.time + 0.12f;
    }

    private void HandleAttacks()
    {
        if (Input.GetMouseButtonDown(0))
            TriggerPrimaryAttack();

        if (Input.GetMouseButtonDown(1))
            TriggerFeintAttack();
    }

    private void TriggerPrimaryAttack()
    {
        SyncCombatSwingForPrimary(currentGuard);
        switch (currentGuard)
        {
            case Guard.Right:
                animator.SetTrigger(swingRLTrigger);
                break;
            case Guard.Left:
                animator.SetTrigger(swingLRTrigger);
                break;
            case Guard.High:
                animator.SetTrigger(slotTrigger);
                break;
        }
    }

    private void TriggerFeintAttack()
    {
        SyncCombatSwingForFeint(currentGuard);
        switch (currentGuard)
        {
            case Guard.Right:
                animator.SetTrigger(swingLRTrigger);
                break;
            case Guard.Left:
                animator.SetTrigger(swingRLTrigger);
                break;
            case Guard.High:
                animator.SetTrigger(swingRLTrigger);
                break;
        }
    }

    private void SetGuard(Guard guard)
    {
        if (currentGuard == guard) return;
        currentGuard = guard;
        SyncCombatGuard(currentGuard);
    }

    private void SyncCombatGuard(Guard guard)
    {
        if (playerCombat == null) return;
        playerCombat.SetGuardType(ToCombatGuardType(guard));
    }

    private void SyncCombatSwingForPrimary(Guard guard)
    {
        if (playerCombat == null) return;
        playerCombat.SetSwingType(ToPrimarySwingType(guard));
    }

    private void SyncCombatSwingForFeint(Guard guard)
    {
        if (playerCombat == null) return;
        playerCombat.SetSwingType(ToFeintSwingType(guard));
    }

    private static PlayerCombat.GuardType ToCombatGuardType(Guard guard)
    {
        switch (guard)
        {
            case Guard.Left:
                return PlayerCombat.GuardType.LeftGuard;
            case Guard.Right:
                return PlayerCombat.GuardType.RightGuard;
            case Guard.High:
            default:
                return PlayerCombat.GuardType.HighGuard;
        }
    }

    private static PlayerCombat.SwingType ToPrimarySwingType(Guard guard)
    {
        switch (guard)
        {
            case Guard.Right:
                return PlayerCombat.SwingType.SwingRL;
            case Guard.Left:
                return PlayerCombat.SwingType.SwingLR;
            case Guard.High:
            default:
                return PlayerCombat.SwingType.Slot;
        }
    }

    private static PlayerCombat.SwingType ToFeintSwingType(Guard guard)
    {
        switch (guard)
        {
            case Guard.Right:
                return PlayerCombat.SwingType.SwingLR;
            case Guard.Left:
                return PlayerCombat.SwingType.SwingRL;
            case Guard.High:
            default:
                return PlayerCombat.SwingType.SwingRL;
        }
    }

    private void ApplyGuardImmediate(Guard guard)
    {
        Vector2 target = GetGuardBlend(guard);
        _currentGSX = target.x;
        _currentGSY = target.y;
        animator.SetFloat(gsxParam, _currentGSX);
        animator.SetFloat(gsyParam, _currentGSY);
    }

    private void UpdateGuardBlend()
    {
        Vector2 target = GetGuardBlend(currentGuard);
        _currentGSX = Mathf.Lerp(_currentGSX, target.x, Time.deltaTime * guardLerpSpeed);
        _currentGSY = Mathf.Lerp(_currentGSY, target.y, Time.deltaTime * guardLerpSpeed);
        animator.SetFloat(gsxParam, _currentGSX);
        animator.SetFloat(gsyParam, _currentGSY);
    }

    private static Vector2 GetGuardBlend(Guard guard)
    {
        switch (guard)
        {
            case Guard.Left:
                return new Vector2(0f, 1f);
            case Guard.Right:
                return new Vector2(-1f, 0f);
            case Guard.High:
            default:
                return new Vector2(1f, 0f);
        }
    }

    private static bool IsMouseButton4Held()
    {
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            return UnityEngine.InputSystem.Mouse.current.backButton.isPressed;
        #endif

        return Input.GetMouseButton(3);
    }
}
