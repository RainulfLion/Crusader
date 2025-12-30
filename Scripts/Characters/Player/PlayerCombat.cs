using UnityEngine;

public class PlayerCombat : MonoBehaviour, ICombatSwingReceiver
{
    public enum SwingType
    {
        None = 0,
        SwingRL = 1,
        SwingLR = 3,
        Slot = 2,
    }

    public enum GuardType
    {
        None = 0,
        HighGuard = 2,
        LeftGuard = 1,
        RightGuard = 3,
    }

    public int swingNumber;

    public CombatResolver combatResolver;

    void Awake()
    {
        if (combatResolver == null)
            combatResolver = GetComponentInParent<CombatResolver>();
    }
    public int guardNumber;

    public void SetSwingNumber(int number)
    {
        swingNumber = number;

        if (combatResolver != null)
            combatResolver.swingNumber = number;
    }

    public void SetSwingType(SwingType type)
    {
        swingNumber = (int)type;

        if (combatResolver != null)
            combatResolver.swingNumber = swingNumber;
    }

    public void SetGuardNumber(int number)
    {
        guardNumber = number;

        if (combatResolver != null)
            combatResolver.guardNumber = guardNumber;
    }

    public void SetGuardType(GuardType type)
    {
        guardNumber = (int)type;

        if (combatResolver != null)
            combatResolver.guardNumber = guardNumber;
    }

    public void ClearSwingNumber()
    {
        swingNumber = 0;

        if (combatResolver != null)
            combatResolver.swingNumber = 0;
    }

    public void ClearGuardNumber()
    {
        guardNumber = 0;

        if (combatResolver != null)
            combatResolver.guardNumber = 0;
    }

    public void SetSwingRL()
    {
        swingNumber = (int)SwingType.SwingRL;

        if (combatResolver != null)
            combatResolver.swingNumber = swingNumber;
    }

    public void SetSwingLR()
    {
        swingNumber = (int)SwingType.SwingLR;

        if (combatResolver != null)
            combatResolver.swingNumber = swingNumber;
    }

    public void SetSlot()
    {
        swingNumber = (int)SwingType.Slot;

        if (combatResolver != null)
            combatResolver.swingNumber = swingNumber;
    }

    public void HighGuard()
    {
        guardNumber = (int)GuardType.HighGuard;

        if (combatResolver != null)
            combatResolver.guardNumber = guardNumber;
    }

    public void LeftGuard()
    {
        guardNumber = (int)GuardType.LeftGuard;

        if (combatResolver != null)
            combatResolver.guardNumber = guardNumber;
    }

    public void RightGuard()
    {
        guardNumber = (int)GuardType.RightGuard;

        if (combatResolver != null)
            combatResolver.guardNumber = guardNumber;
    }
}
