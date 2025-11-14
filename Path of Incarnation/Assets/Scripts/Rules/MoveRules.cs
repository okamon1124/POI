using System;
using UnityEngine;

public enum MoveType { Player, System, Effect }

public static class MoveRules
{
    public static bool CanMoveZoneToZone(UiCard card, UiZone from, UiZone to, MoveType moveType, out string reason)
    {
        if (!card) { reason = "Card missing."; return false; }
        if (!card.CardData) { reason = "Card has no data."; return false; }
        if (!from) { reason = "Source missing."; return false; }
        if (!to) { reason = "Destination missing."; return false; }
        if (from == to) { reason = "Same zone."; return false; }

        if (moveType == MoveType.System)
        {
            if (from.nextZone != to) { reason = "No next step."; return false; }
            if (to.zoneType != ZoneType.Hand && to.IsFull)
            {
                reason = "Destination full.";
                return false;
            }

            reason = null;
            return true;
        }

        // Player / Effect use per-card rules (still fetched from CardData)
        var rules = moveType switch
        {
            MoveType.Player => card.CardData.playerMoveRules,
            MoveType.Effect => card.CardData.playerMoveRules, // later you can separate this
            _ => card.CardData.playerMoveRules
        } ?? System.Array.Empty<PlayRule>();

        // 🔴 Call the logic-only evaluator now:
        return PlayRuleEval.Evaluate(
            rules,
            card.CardData,
            from.zoneType,
            to.zoneType,
            moveType,
            out reason
        );
    }
}