using UnityEngine;

/// <summary>
/// Extension to Board for mana spending logic.
/// Call this after a successful card play from hand.
/// </summary>
public static class BoardManaExtensions
{
    /// <summary>
    /// Spend mana when playing a card from hand.
    /// Should be called AFTER move validation succeeds.
    /// </summary>
    public static void SpendManaForCard(CardInstance card, Slot fromSlot, MoveType moveType)
    {
        // Only spend mana for player moves from hand
        if (moveType != MoveType.Player) return;
        if (fromSlot == null || fromSlot.Zone.Type != ZoneType.Hand) return;

        // Get the appropriate mana system
        ManaSystem manaSystem = card.Owner == Owner.Player
            ? MoveRules.PlayerManaSystem
            : MoveRules.OpponentManaSystem;

        if (manaSystem == null)
        {
            Debug.LogWarning($"[BoardManaExtensions] No ManaSystem for {card.Owner}!");
            return;
        }

        int cost = card.Data.manaCost;

        if (!manaSystem.TrySpendMana(cost, out string reason))
        {
            // This shouldn't happen if CanMove validated properly
            Debug.LogError($"[BoardManaExtensions] Failed to spend mana after move succeeded! {reason}");
        }
    }
}