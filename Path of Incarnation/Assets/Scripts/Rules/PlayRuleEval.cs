public static class PlayRuleEval
{
    public static bool Evaluate(
        PlayRule[] rules,
        CardData cardData,
        ZoneType fromZone,
        ZoneType toZone,
        MoveType moveType,
        out string reason
    )
    {
        reason = null;

        // Require at least one explicit allow
        if (rules == null || rules.Length == 0)
        {
            return false;
        }

        bool anyAllow = false;

        foreach (var r in rules)
        {
            if (!r) continue;

            var res = r.Evaluate(cardData, fromZone, toZone, moveType, out var rReason);

            if (res == RuleResult.Deny)
            {
                reason = rReason;
                return false;
            }

            if (res == RuleResult.Allow)
            {
                anyAllow = true;
            }
        }

        if (anyAllow) return true;

        reason = "No rule allowed this move.";
        return false;
    }
}