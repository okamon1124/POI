using UnityEngine;

[CreateAssetMenu(menuName = "Card Game/Rules/ZoneRoute")]
public class ZoneRouteRule : PlayRule
{
    [SerializeField] private ZoneType fromType;
    [SerializeField] private ZoneType toType;

    public override RuleResult Evaluate(
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    )
    {
        // Rule only matches if both zone types match what we expect
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