using UnityEngine;

public class PlayerCombatAnimationEventRelay : CombatAnimationEventRelay
{
    public PlayerCombat playerCombat;

    protected override Component GetDefaultCombatComponent()
    {
        CombatResolver resolver = GetComponentInParent<CombatResolver>();
        if (resolver != null)
            return resolver;

        if (playerCombat != null)
            return playerCombat;

        playerCombat = GetComponentInParent<PlayerCombat>();
        return playerCombat;
    }
}
