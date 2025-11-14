using UnityEngine;

public enum RuleResult { Allow, Deny, Ignore }

// Logic-level interface: no UiCard / UiZone here
public interface IPlayRule
{
    RuleResult Evaluate(
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    );
}

public abstract class PlayRule : ScriptableObject, IPlayRule
{
    public abstract RuleResult Evaluate(
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    );
}