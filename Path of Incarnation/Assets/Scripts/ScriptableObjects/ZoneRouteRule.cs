using UnityEngine;

[CreateAssetMenu(menuName = "Card Game/Rules/ZoneRoute")]
public class ZoneRouteRule : PlayRule
{
    [SerializeField] private ZoneType fromType;
    [SerializeField] private ZoneType toType;
    [SerializeField] private Team allowedSide = Team.Ally; // Ally, Enemy, Both

    public override RuleResult Evaluate(UiCard card, Zone from, Zone to, MoveType moveType, out string reason)
    {
        if (from.zoneType != fromType || to.zoneType != toType) { reason = null; return RuleResult.Ignore; }

        if (allowedSide == Team.Both) { reason = null; return RuleResult.Allow; }

        bool isAllyTarget = (card.OwnerTeam == to.OwnerTeam);
        bool ok = (allowedSide == Team.Ally && isAllyTarget) ||
                  (allowedSide == Team.Enemy && !isAllyTarget);

        if (ok) { reason = null; return RuleResult.Allow; }

        reason = "Wrong side for this move.";
        return RuleResult.Deny;
    }
}
