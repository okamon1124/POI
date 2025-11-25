using UnityEngine;

public abstract class PlayRule : ScriptableObject
{
    /// <summary>
    /// Evaluate the rule for a card moving from one zone to another.
    /// Return RuleResult.Ignore if this rule does not apply.
    /// </summary>
    public abstract RuleResult Evaluate(
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    );
}