using UnityEngine;

[CreateAssetMenu(menuName = "Card Game/Rules/Zone Route")]
public class ZoneRouteRule : PlayRule
{
    [SerializeField] private ZoneType fromType;
    [SerializeField] private ZoneType toType;

    /// <summary>
    /// Allows a move only if it is a Player/Effect move AND the route is exactly fromType → toType.
    /// Otherwise this rule is ignored (does not deny).
    /// </summary>
    public override RuleResult Evaluate(
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    )
    {
        // This rule is only meant for Player/Effect moves, not System auto-steps.
        if (moveType == MoveType.System)
        {
            reason = null;
            return RuleResult.Ignore;
        }

        // Rule only applies if both zone types match exactly
        if (fromZone != fromType || toZone != toType)
        {
            reason = null;
            return RuleResult.Ignore;
        }

        // Route matches → allow
        reason = null;
        return RuleResult.Allow;
    }
}
