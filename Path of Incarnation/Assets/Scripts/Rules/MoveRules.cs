using System;
using UnityEngine;

public enum MoveType { Player, System, Effect }

public static class MoveRules
{
    public static bool CanMoveZoneToZone(UiCard card, Zone from, Zone to, MoveType moveType, out string reason)
    {
        if (!card) { reason = "Card missing."; return false; }
        if (!card.CardData) { reason = "Card has no data."; return false; }
        if (!from) { reason = "Source missing."; return false; }
        if (!to) { reason = "Destination missing."; return false; }
        if (from == to) { reason = "Same zone."; return false; }

        // ✅ System fallback: follow nextZone, obey capacity
        if (moveType == MoveType.System)
        {
            if (from.nextZone != to) { reason = "No next step."; return false; }
            if (to.zoneType != ZoneType.Hand && to.IsFull) { reason = "Destination full."; return false; }
            reason = null;
            return true;
        }

        // Player / Effect use per-card rules
        var rules = moveType switch
        {
            MoveType.Player => card.CardData.playerMoveRules,
            MoveType.Effect => card.CardData.playerMoveRules, // or a separate list later
            _ => card.CardData.playerMoveRules
        } ?? System.Array.Empty<PlayRule>();

        return PlayRuleEval.Evaluate(rules, card, from, to, moveType, out reason);
    }
}