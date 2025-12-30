using UnityEngine;

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

    [Header("State")]
    public Guard currentGuard = Guard.High;
    public bool defending;

    private float _currentGSX;
    private float _currentGSY;

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
    }

    private void SyncCombatResolver()
    {
        if (combatResolver == null) return;
        combatResolver.SetDefending(defending);
        combatResolver.SetGuard(ToCombatGuard(currentGuard));
    }

    public void SetGuard(Guard guard)
    {
        currentGuard = guard;
        SyncCombatResolver();
    }

    public void TriggerPrimaryAttack()
    {
        if (animator == null) return;

        if (combatResolver != null)
            combatResolver.SetSwing(ToPrimarySwing(currentGuard));

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

        if (animator == null) return;
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
}
