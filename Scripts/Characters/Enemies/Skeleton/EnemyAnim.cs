using UnityEngine;
using UnityEngine.Events;

public class EnemyAnim : MonoBehaviour
{
    public enum Guard
    {
        Left,
        High,
        Right,
    }

    [Header("References")]
    public Animator animator;
    public CombatResolver combatResolver;

    [Header("Animator Params")]
    public string gsxParam = "GSX";
    public string gsyParam = "GSY";
    public string defendingBoolParam = "Defending";
    public string slotTrigger = "Slot";
    public string swingLRTrigger = "SwingLR";
    public string swingRLTrigger = "SwingRL";

    [Header("Tuning")]
    public float guardLerpSpeed = 10f;
    [Tooltip("How close the blend values need to be to target before considered 'settled'")]
    public float guardSettleThreshold = 0.05f;

    [Header("State")]
    public Guard currentGuard = Guard.High;
    public bool defending;
    
    [Header("Debug")]
    public bool isGuardSettled = true;
    public bool showDebugLogs;

    [Header("Events")]
    public UnityEvent<Guard> onGuardChanged;
    public UnityEvent onGuardSettled;
    public UnityEvent onAttackTriggered;

    private float _currentGSX;
    private float _currentGSY;
    private float _targetGSX;
    private float _targetGSY;
    private bool _wasSettled = true;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (combatResolver == null) combatResolver = GetComponentInParent<CombatResolver>();

        if (combatResolver != null)
        {
            combatResolver.readDefenseFromAnimator = false;
            if (combatResolver.animator == null)
                combatResolver.animator = animator;
        }

        ApplyGuardImmediate(currentGuard);
    }

    void Update()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(defendingBoolParam))
            animator.SetBool(defendingBoolParam, defending);

        SyncCombatResolver();
        UpdateGuardBlend();
        CheckGuardSettled();
    }

    private void SyncCombatResolver()
    {
        if (combatResolver == null) return;
        combatResolver.SetDefending(defending);
        combatResolver.SetGuard(ToCombatGuard(currentGuard));
    }

    public void SetGuard(Guard guard)
    {
        if (currentGuard == guard) return;
        
        Guard oldGuard = currentGuard;
        currentGuard = guard;
        
        Vector2 target = GetGuardBlend(guard);
        _targetGSX = target.x;
        _targetGSY = target.y;
        isGuardSettled = false;
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Guard changed: {oldGuard} -> {guard}", this);
        
        SyncCombatResolver();
        onGuardChanged?.Invoke(guard);
    }
    
    public void SetGuardImmediate(Guard guard)
    {
        currentGuard = guard;
        ApplyGuardImmediate(guard);
        SyncCombatResolver();
        onGuardChanged?.Invoke(guard);
    }

    public void TriggerPrimaryAttack()
    {
        if (animator == null) return;

        if (combatResolver != null)
            combatResolver.SetSwing(ToPrimarySwing(currentGuard));

        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] Attack triggered from guard: {currentGuard}", this);

        switch (currentGuard)
        {
            case Guard.Left:
                animator.SetTrigger(swingLRTrigger);
                break;
            case Guard.Right:
                animator.SetTrigger(swingRLTrigger);
                break;
            case Guard.High:
            default:
                animator.SetTrigger(slotTrigger);
                break;
        }
        
        onAttackTriggered?.Invoke();
    }

    private static CombatResolver.Guard ToCombatGuard(Guard guard)
    {
        switch (guard)
        {
            case Guard.Left:
                return CombatResolver.Guard.Left;
            case Guard.Right:
                return CombatResolver.Guard.Right;
            case Guard.High:
            default:
                return CombatResolver.Guard.High;
        }
    }

    private static CombatResolver.Swing ToPrimarySwing(Guard guard)
    {
        switch (guard)
        {
            case Guard.Left:
                return CombatResolver.Swing.SwingLR;
            case Guard.Right:
                return CombatResolver.Swing.SwingRL;
            case Guard.High:
            default:
                return CombatResolver.Swing.Slot;
        }
    }

    private void ApplyGuardImmediate(Guard guard)
    {
        Vector2 target = GetGuardBlend(guard);
        _currentGSX = target.x;
        _currentGSY = target.y;
        _targetGSX = target.x;
        _targetGSY = target.y;
        isGuardSettled = true;

        if (animator == null) return;
        animator.SetFloat(gsxParam, _currentGSX);
        animator.SetFloat(gsyParam, _currentGSY);
    }

    private void UpdateGuardBlend()
    {
        Vector2 current = new Vector2(_currentGSX, _currentGSY);
        Vector2 target = new Vector2(_targetGSX, _targetGSY);

        current = Vector2.MoveTowards(current, target, guardLerpSpeed * Time.deltaTime);
        _currentGSX = current.x;
        _currentGSY = current.y;

        animator.SetFloat(gsxParam, _currentGSX);
        animator.SetFloat(gsyParam, _currentGSY);
    }
    
    private void CheckGuardSettled()
    {
        float dx = Mathf.Abs(_currentGSX - _targetGSX);

        float dy = Mathf.Abs(_currentGSY - _targetGSY);
        
        bool settled = dx < guardSettleThreshold && dy < guardSettleThreshold;
        
        if (settled && !_wasSettled)
        {
            isGuardSettled = true;
            onGuardSettled?.Invoke();
            
            if (showDebugLogs)
                Debug.Log($"[{gameObject.name}] Guard settled at: {currentGuard}", this);
        }
        
        _wasSettled = settled;
        isGuardSettled = settled;
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
    
    // Utility for external queries
    public bool IsGuardSettled() => isGuardSettled;
    public Guard GetCurrentGuard() => currentGuard;

    public void ForceSyncCombatResolver()
    {
        if (combatResolver == null) return;
        
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] ForceSyncCombatResolver: defending={defending}, guard={currentGuard}", this);
        
        combatResolver.ForceSync(defending, (int)ToCombatGuard(currentGuard));
    }

    public void OnTookDamage()
    {
        if (showDebugLogs)
            Debug.Log($"[{gameObject.name}] OnTookDamage - resetting to Guard.High and syncing", this);
        
        SetGuardImmediate(Guard.High);
        defending = false;
        ForceSyncCombatResolver();
    }
}