using UnityEngine;

public class CombatResolver : MonoBehaviour, ICombatSwingReceiver
{
    public enum Guard
    {
        Left = 1,
        High = 2,
        Right = 3,
    }

    public enum Swing
    {
        SwingRL = 1,
        Slot = 2,
        SwingLR = 3,
    }

    public enum Outcome
    {
        Hit = 0,
        Blocked = 1,
    }

    [Header("State")]
    public bool defending;
    public int guardNumber;
    public int swingNumber;

    [Header("Animator Input (Optional)")]
    [Tooltip("If true, reads guard/defending from animator. Set to FALSE if using EnemyAnim or other scripts that control guard directly.")]
    public bool readDefenseFromAnimator = false;
    public Animator animator;
    public string defendingBoolParam = "Defending";
    public string gsxParam = "GSX";
    public string gsyParam = "GSY";
    [Range(0f, 1f)]
    public float guardAxisThreshold = 0.25f;
    
    [Header("Blend Stability")]
    [Tooltip("Only update guard from animator when blend values are stable (not mid-transition)")]
    public bool requireStableBlendForGuardUpdate = true;
    [Tooltip("How much the blend values can change per frame before considered 'unstable'")]
    public float blendStabilityThreshold = 0.1f;

    [Header("Swing Safety")]
    public bool autoClearSwing;
    public float autoClearSwingAfter = 0.35f;

    [Header("Debug")]
    public bool showDebugLogs;

    private int _defendingHash;
    private int _gsxHash;
    private int _gsyHash;
    private bool _hasDefending;
    private bool _hasGsx;
    private bool _hasGsy;
    
    private float _lastGsx;
    private float _lastGsy;
    private bool _blendIsStable;

    private int _lastSwingNumber;
    private float _swingStartTime;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorParams();

        _lastSwingNumber = swingNumber;
        _swingStartTime = swingNumber > 0 ? Time.time : 0f;
    }

    void Update()
    {
        if (_lastSwingNumber <= 0 && swingNumber > 0)
            _swingStartTime = Time.time;

        if (swingNumber <= 0)
            _swingStartTime = 0f;

        if (autoClearSwing && swingNumber > 0 && autoClearSwingAfter > 0f)
        {
            if (_swingStartTime > 0f && Time.time - _swingStartTime >= autoClearSwingAfter)
            {
                if (showDebugLogs)
                    Debug.Log($"[CombatResolver] Auto-clearing stuck swing: swing={swingNumber} elapsed={(Time.time - _swingStartTime):F3}s", this);
                swingNumber = 0;
                _swingStartTime = 0f;
            }
        }

        _lastSwingNumber = swingNumber;
    }

    void LateUpdate()
    {
        if (!readDefenseFromAnimator) return;
        if (animator == null) return;

        if (_hasDefending)
            defending = animator.GetBool(_defendingHash);

        if (_hasGsx && _hasGsy)
        {
            float gsx = animator.GetFloat(_gsxHash);
            float gsy = animator.GetFloat(_gsyHash);
            
            // Check if blend values are stable (not mid-lerp)
            if (requireStableBlendForGuardUpdate)
            {
                float deltaGsx = Mathf.Abs(gsx - _lastGsx);
                float deltaGsy = Mathf.Abs(gsy - _lastGsy);
                _blendIsStable = deltaGsx < blendStabilityThreshold && deltaGsy < blendStabilityThreshold;
                
                _lastGsx = gsx;
                _lastGsy = gsy;
                
                // Only update guard when blend is stable
                if (!_blendIsStable)
                {
                    if (showDebugLogs)
                        Debug.Log($"[CombatResolver] Skipping guard update - blend unstable (delta: {deltaGsx:F3}, {deltaGsy:F3})", this);
                    return;
                }
            }
            
            Guard newGuard = GuardFromBlend(gsx, gsy);
            int newGuardNumber = (int)newGuard;
            
            if (showDebugLogs && newGuardNumber != guardNumber)
                Debug.Log($"[CombatResolver] Guard updated from animator: {guardNumber} -> {newGuardNumber} (GSX:{gsx:F2} GSY:{gsy:F2})", this);
            
            guardNumber = newGuardNumber;
        }
    }

    private void CacheAnimatorParams()
    {
        _hasDefending = false;
        _hasGsx = false;
        _hasGsy = false;

        if (animator == null) return;

        _defendingHash = Animator.StringToHash(defendingBoolParam);
        _gsxHash = Animator.StringToHash(gsxParam);
        _gsyHash = Animator.StringToHash(gsyParam);

        AnimatorControllerParameter[] ps = animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            AnimatorControllerParameter p = ps[i];
            if (!_hasDefending && p.type == AnimatorControllerParameterType.Bool && p.name == defendingBoolParam)
                _hasDefending = true;
            else if (!_hasGsx && p.type == AnimatorControllerParameterType.Float && p.name == gsxParam)
                _hasGsx = true;
            else if (!_hasGsy && p.type == AnimatorControllerParameterType.Float && p.name == gsyParam)
                _hasGsy = true;
        }
    }

    private Guard GuardFromBlend(float gsx, float gsy)
    {
        // Left guard: GSX=0, GSY=1
        if (Mathf.Abs(gsy) > Mathf.Abs(gsx) && gsy >= guardAxisThreshold)
            return Guard.Left;

        // If values are too small, keep current guard
        if (Mathf.Abs(gsx) < guardAxisThreshold && Mathf.Abs(gsy) < guardAxisThreshold)
            return (Guard)Mathf.Clamp(guardNumber, 1, 3);

        // High guard: GSX=1, GSY=0
        // Right guard: GSX=-1, GSY=0
        return gsx >= 0f ? Guard.High : Guard.Right;
    }

    public void SetDefending(bool isDefending)
    {
        defending = isDefending;
        
        if (showDebugLogs)
            Debug.Log($"[CombatResolver] SetDefending: {isDefending}", this);
    }

    public void SetGuard(Guard guard)
    {
        int newGuardNumber = (int)guard;
        
        if (showDebugLogs && newGuardNumber != guardNumber)
            Debug.Log($"[CombatResolver] SetGuard: {(Guard)guardNumber} -> {guard}", this);
        
        guardNumber = newGuardNumber;
    }

    public void SetSwing(Swing swing)
    {
        int newSwingNumber = (int)swing;
        
        if (showDebugLogs)
            Debug.Log($"[CombatResolver] SetSwing: {swing} (number: {newSwingNumber})", this);
        
        swingNumber = newSwingNumber;
    }

    public static int GuardToNumber(Guard guard)
    {
        return (int)guard;
    }

    public static int SwingToNumber(Swing swing)
    {
        return (int)swing;
    }

    public static int PrimarySwingNumberForGuard(Guard guard)
    {
        switch (guard)
        {
            case Guard.Left:
                return (int)Swing.SwingLR;
            case Guard.Right:
                return (int)Swing.SwingRL;
            case Guard.High:
            default:
                return (int)Swing.Slot;
        }
    }

    public static Guard RequiredGuardForSwingNumber(int swingNumberValue)
    {
        switch ((Swing)swingNumberValue)
        {
            case Swing.SwingLR:
                return Guard.Left;
            case Swing.SwingRL:
                return Guard.Right;
            case Swing.Slot:
            default:
                return Guard.High;
        }
    }

    public static Outcome ResolveAttack(int attackerSwingNumber, bool defenderDefending, int defenderGuardNumber)
    {
        if (attackerSwingNumber <= 0)
            return Outcome.Hit;

        if (!defenderDefending)
            return Outcome.Hit;

        if (defenderGuardNumber <= 0)
            return Outcome.Hit;

        Guard required = RequiredGuardForSwingNumber(attackerSwingNumber);
        return defenderGuardNumber == (int)required ? Outcome.Blocked : Outcome.Hit;
    }

    public Outcome ResolveAttackFrom(CombatResolver attacker)
    {
        if (attacker == null) return Outcome.Hit;
        
        Outcome result = ResolveAttack(attacker.swingNumber, defending, guardNumber);
        
        if (showDebugLogs)
        {
            Guard requiredGuard = RequiredGuardForSwingNumber(attacker.swingNumber);
            Debug.Log($"[CombatResolver] ResolveAttackFrom: swing={attacker.swingNumber} ({(Swing)attacker.swingNumber}) requires guard={requiredGuard}, defender defending={defending} guard={guardNumber} ({(Guard)guardNumber}) -> {result}", this);
        }
        
        return result;
    }

    public void SetSwingNumber(int number)
    {
        swingNumber = number;
    }

    public void SetSwingRL()
    {
        swingNumber = (int)Swing.SwingRL;
    }

    public void SetSwingLR()
    {
        swingNumber = (int)Swing.SwingLR;
    }

    public void SetSlot()
    {
        swingNumber = (int)Swing.Slot;
    }

    public void ClearSwingNumber()
    {
        swingNumber = 0;
    }

    public void HighGuard()
    {
        guardNumber = (int)Guard.High;
    }

    public void LightGuard()
    {
        guardNumber = (int)Guard.High;
    }

    public void LeftGuard()
    {
        guardNumber = (int)Guard.Left;
    }

    public void RightGuard()
    {
        guardNumber = (int)Guard.Right;
    }

    public void ResetCombatState(bool resetDefending = true, Guard resetGuard = Guard.High)
    {
        if (showDebugLogs)
            Debug.Log($"[CombatResolver] ResetCombatState called. defending={resetDefending}, guard={resetGuard}", this);
        
        defending = resetDefending;
        guardNumber = (int)resetGuard;
        swingNumber = 0;
        
        _lastGsx = 0f;
        _lastGsy = 0f;
        _blendIsStable = false;
    }

    public void ResetToDefending(Guard guard)
    {
        if (showDebugLogs)
            Debug.Log($"[CombatResolver] ResetToDefending: guard={guard}", this);
        
        defending = true;
        guardNumber = (int)guard;
        swingNumber = 0;
    }

    public void ForceSync(bool isDefending, int guard)
    {
        if (showDebugLogs)
            Debug.Log($"[CombatResolver] ForceSync: defending={isDefending}, guard={guard}", this);
        
        defending = isDefending;
        guardNumber = guard;
    }
}


