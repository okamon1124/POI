using UnityEngine;

public enum RuleResult { Allow, Deny, Ignore }

// Replace old interface/abstract:
public interface IPlayRule
{
    RuleResult Evaluate(UiCard card, Zone from, Zone to, MoveType moveType, out string reason);
}

public abstract class PlayRule : ScriptableObject, IPlayRule
{
    public abstract RuleResult Evaluate(UiCard card, Zone from, Zone to, MoveType moveType, out string reason);
}

// Evaluator: Deny > Allow > Ignore
public static class PlayRuleEval
{
    public static bool Evaluate(PlayRule[] rules, UiCard card, Zone from, Zone to, MoveType moveType, out string reason)
    {
        reason = null;
        if (rules == null || rules.Length == 0) return false;         // require an explicit allow

        bool anyAllow = false;
        foreach (var r in rules)
        {
            if (!r) continue;
            var res = r.Evaluate(card, from, to, moveType, out var rReason);
            if (res == RuleResult.Deny) { reason = rReason; return false; }
            if (res == RuleResult.Allow) anyAllow = true;
        }

        if (anyAllow) return true;
        reason = "No rule allowed this move.";
        return false;
    }
}
