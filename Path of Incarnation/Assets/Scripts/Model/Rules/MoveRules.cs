using System;
using UnityEngine;

public static class MoveRules
{
    // Injected dependency - set by GameController
    public static ManaSystem PlayerManaSystem { get; set; }
    public static ManaSystem OpponentManaSystem { get; set; }

    public static bool CanMove(
        CardInstance card,
        Slot fromSlot,
        Slot toSlot,
        MoveType moveType,
        out string reason)
    {
        // ---- Basic safety ----
        if (card == null)
        {
            reason = "Card missing.";
            return false;
        }
        if (card.Data == null)
        {
            reason = "Card has no data.";
            return false;
        }
        if (fromSlot == null)
        {
            reason = "Source missing.";
            return false;
        }
        if (toSlot == null)
        {
            reason = "Destination missing.";
            return false;
        }
        if (fromSlot == toSlot)
        {
            reason = "Same slot.";
            return false;
        }

        var fromZone = fromSlot.Zone;
        var toZone = toSlot.Zone;

        if (fromZone == null)
        {
            reason = "Source zone missing.";
            return false;
        }
        if (toZone == null)
        {
            reason = "Destination zone missing.";
            return false;
        }

        // Cannot move to opponent zone
        if (toZone.Owner != card.Owner)
        {
            reason = "Cannot move to opponent zone.";
            return false;
        }

        // Cannot move from opponent zone
        if (fromZone.Owner != card.Owner)
        {
            reason = "Cannot move from opponent zone.";
            return false;
        }

        // ---- MANA CHECK (for player moves from hand) ----
        if (moveType == MoveType.Player && fromZone.Type == ZoneType.Hand)
        {
            // Get the appropriate mana system
            ManaSystem manaSystem = card.Owner == Owner.Player
                ? PlayerManaSystem
                : OpponentManaSystem;

            if (manaSystem != null)
            {
                int cost = card.Data.manaCost;

                if (!manaSystem.CanAfford(cost))
                {
                    reason = $"Not enough mana. Need {cost}, have {manaSystem.CurrentMana}.";
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[MoveRules] ManaSystem not set for {card.Owner}!");
            }
        }

        // ---- System moves: auto lane logic ----
        if (moveType == MoveType.System)
        {
            // follow the lane: Deployment -> Advance -> Combat, etc.
            if (fromSlot.NextSlot != toSlot)
            {
                reason = "No next step.";
                return false;
            }

            // Basic "full" check: any non-Hand slot must be empty
            if (toZone.Type != ZoneType.Hand && !toSlot.IsEmpty)
            {
                reason = "Destination full.";
                return false;
            }

            reason = null;
            return true;
        }

        // ---- Player / Effect moves: use per-card rules from CardData ----
        var rules = moveType switch
        {
            MoveType.Player => card.Data.playerMoveRules,
            MoveType.Effect => card.Data.playerMoveRules, // later: separate effectMoveRules if you want
            _ => card.Data.playerMoveRules
        } ?? Array.Empty<PlayRule>();

        return PlayRuleEval.Evaluate(
            rules,
            card.Data,
            fromZone.Type,
            toZone.Type,
            moveType,
            out reason
        );
    }
}