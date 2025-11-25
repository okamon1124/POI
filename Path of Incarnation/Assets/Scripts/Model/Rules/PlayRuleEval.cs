public static class PlayRuleEval
{
    public static bool Evaluate(
        PlayRule[] rules,
        CardData data,
        ZoneType from,
        ZoneType to,
        MoveType moveType,
        out string reason)
    {
        bool anyAllow = false;
        reason = null;

        foreach (var rule in rules)
        {
            if (rule == null)
                continue;

            var result = rule.Evaluate(data, from, to, moveType, out var localReason);

            switch (result)
            {
                case RuleResult.Ignore:
                    continue;

                case RuleResult.Allow:
                    anyAllow = true;
                    continue;

                case RuleResult.Deny:
                    reason = localReason ?? "Move denied.";
                    return false;
            }
        }

        if (!anyAllow)
        {
            reason = "No rule allows this move.";
            return false;
        }

        return true;
    }
}
