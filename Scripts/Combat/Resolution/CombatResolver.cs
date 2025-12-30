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
    public bool readDefenseFromAnimator = true;
    public Animator animator;
    public string defendingBoolParam = "Defending";
    public string gsxParam = "GSX";
    public string gsyParam = "GSY";
    [Range(0f, 1f)]
    public float guardAxisThreshold = 0.25f;

    private int _defendingHash;
    private int _gsxHash;
    private int _gsyHash;
    private bool _hasDefending;
    private bool _hasGsx;
    private bool _hasGsy;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorParams();
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
            guardNumber = (int)GuardFromBlend(gsx, gsy);
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
        if (Mathf.Abs(gsy) > Mathf.Abs(gsx) && Mathf.Abs(gsy) >= guardAxisThreshold)
            return Guard.Left;

        if (Mathf.Abs(gsx) < guardAxisThreshold)
            return (Guard)Mathf.Clamp(guardNumber, 1, 3);

        return gsx >= 0f ? Guard.High : Guard.Right;
    }

    public void SetDefending(bool isDefending)
    {
        defending = isDefending;
    }

    public void SetGuard(Guard guard)
    {
        guardNumber = (int)guard;
    }

    public void SetSwing(Swing swing)
    {
        swingNumber = (int)swing;
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
        return ResolveAttack(attacker.swingNumber, defending, guardNumber);
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
}
