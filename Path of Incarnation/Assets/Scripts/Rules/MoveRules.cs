using UnityEngine;

public enum MoveType { Player, System, Effect }

public static class MoveRules
{
    public static bool CanMoveZoneToZone(UiCard card, Zone from, Zone to, MoveType moveType, out string reason)
    {
        // --- Basic safety (always on) ---
        if (card == null) { reason = "Card is missing."; return false; }
        if (from == null) { reason = "Source zone missing."; return false; }
        if (to == null) { reason = "Destination zone missing."; return false; }
        if (from == to) { reason = "Source and destination are the same zone."; return false; }

        // Capacity check (hands typically large; board cells usually capacity 1)
        if (to.zoneType != ZoneType.Hand && to.IsFull)
        {
            reason = "Destination is occupied.";
            return false;
        }

        var cardType = card.CardData.cardType;

        // --- Special board progression (Advance/Combat) ---
        // Only System/Effect can move cards into Advance or Combat (typically creatures advancing)
        if (to.zoneType == ZoneType.Advance || to.zoneType == ZoneType.Combat)
        {
            if (moveType == MoveType.System || moveType == MoveType.Effect)
            {
                if (cardType != CardType.Creature)
                {
                    reason = "Only creature cards may advance to this zone.";
                    return false;
                }
                reason = null;
                return true;
            }
            reason = "Only system/effects can move a card to Advance/Combat.";
            return false;
        }

        // --- Player-driven moves ---
        if (moveType == MoveType.Player)
        {
            // Hand ¡÷ Main (Creature/Object)
            if (from.zoneType == ZoneType.Hand && to.zoneType == ZoneType.Main)
            {
                if (cardType == CardType.Creature || cardType == CardType.Object) { reason = null; return true; }
                reason = "Only Creature or Object can move from Hand to Main."; return false;
            }

            // Hand ¡÷ Environment (Environment)
            if (from.zoneType == ZoneType.Hand && to.zoneType == ZoneType.Environment)
            {
                if (cardType == CardType.Environment) { reason = null; return true; }
                reason = "Only Environment cards can move to Environment zone."; return false;
            }

            // Hand ¡÷ Equipment (Equipment)
            if (from.zoneType == ZoneType.Hand && to.zoneType == ZoneType.Equipment)
            {
                if (cardType == CardType.Equipment) { reason = null; return true; }
                reason = "Only Equipment cards can move to Equipment zone."; return false;
            }

            // Hand ¡÷ Deployment (Creature)
            if (from.zoneType == ZoneType.Hand && to.zoneType == ZoneType.Deployment)
            {
                if (cardType == CardType.Creature) { reason = null; return true; }
                reason = "Only Creature cards can move from Hand to Deployment."; return false;
            }

            // Main ¡÷ Deployment (creatures only)
            if (from.zoneType == ZoneType.Main && to.zoneType == ZoneType.Deployment)
            {
                if (cardType == CardType.Creature) { reason = null; return true; }
                reason = "Only Creature cards can move from Main to Deployment."; return false;
            }

            // No other board¡÷board moves by player (for now)
            reason = "Players cannot move cards between these zones.";
            return false;
        }

        // --- System/Effect default policy ---
        // System/Effect moves bypass player routing rules but still respect basic safety/capacity.
        // If you want to restrict further, add specific cases above.
        reason = null;
        return true;
    }
}
