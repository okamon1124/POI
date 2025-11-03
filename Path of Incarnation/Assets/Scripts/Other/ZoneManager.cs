using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[DefaultExecutionOrder(-200)]
public class ZoneManager : Singleton<ZoneManager>
{
    private readonly List<Zone> zones = new();
    public IReadOnlyList<Zone> Zones => zones;

    // ---- Registration from Zone.OnEnable/OnDisable ----
    public void Register(Zone z)
    {
        if (z && !zones.Contains(z)) zones.Add(z);
    }

    public void Unregister(Zone z)
    {
        if (z) zones.Remove(z);
    }

    // ---- Queries ----
    public Zone GetZoneOfType(ZoneType type) => zones.Find(z => z.zoneType == type);

    public List<Zone> GetZonesOfType(ZoneType type)
    {
        var list = new List<Zone>();
        foreach (var z in zones) if (z.zoneType == type) list.Add(z);
        return list;
    }

    // Gate starting a player drag/click (uses MoveRules)
    public bool HasValidDestination(UiCard card)
    {
        if (card == null || card.CurrentZone == null) return false;

        foreach (var z in zones)
        {
            if (z == card.CurrentZone) continue;
            if (z.IsFull) continue;
            if (MoveRules.CanMoveZoneToZone(card, card.CurrentZone, z, MoveType.Player, out _))
                return true;
        }
        return false;
    }

    public List<Zone> GetValidDestinations(UiCard card)
    {
        var list = new List<Zone>();
        if (card == null || card.CurrentZone == null) return list;

        foreach (var z in zones)
        {
            if (z == card.CurrentZone) continue;
            if (z.IsFull) continue;
            if (MoveRules.CanMoveZoneToZone(card, card.CurrentZone, z, MoveType.Player, out _))
                list.Add(z);
        }
        return list;
    }

    // ---- Movement (single authority) ----
    // Convenience overload: infer 'from' from the card
    public bool TryMoveCard(UiCard card, Zone to, MoveType moveType, out string reason)
        => TryMoveCard(card, card?.CurrentZone, to, moveType, out reason);

    public bool TryMoveCard(UiCard card, Zone from, Zone to, MoveType moveType, out string reason)
    {
        Debug.Log("TryMoveCard");
        
        reason = null;

        // Basic safety
        if (card == null) { reason = "Invalid move: card is null."; return false; }
        if (from == null) { reason = "Invalid move: source zone is null."; return false; }
        if (to == null) { reason = "Invalid move: destination zone is null."; return false; }

        // Rules check
        if (!MoveRules.CanMoveZoneToZone(card, from, to, moveType, out reason))
        {
            Debug.LogWarning($"[ZoneManager] Move rejected: {reason}");
            return false;
        }

        // Commit with rollback safety
        if (!from.Remove(card))
        {
            reason = "Failed to remove card from source.";
            return false;
        }

        if (!to.TryAdd(card))
        {
            // rollback if destination became full or changed
            from.TryAdd(card);
            reason = "Destination became full.";
            return false;
        }

        // Point the card to its new zone (visuals handled by card FSM/caller)
        card.AssignZone(to);

        Debug.Log($"<color=cyan>[ZoneManager]</color> {card.name} " +
                  $"<color=yellow>{from.zoneType}</color> ¡÷ <color=lime>{to.zoneType}</color> ({moveType})");

        return true;
    }

    // System-move a single card along its zone's nextZone (1 step)
    public bool AdvanceOneStep(UiCard card, out string reason)
    {
        reason = null;
        var from = card?.CurrentZone;
        var to = from ? from.nextZone : null;
        if (!from || !to) { reason = "No next step."; return false; }

        var ok = TryMoveCard(card, from, to, MoveType.System, out reason);
        if (ok) card.AnimateToCurrentZone();   // reuse your tween
        return ok;
    }

    // Advance everyone exactly 1 step wherever a nextZone exists.
    // (No need to specify a starting zone.)
    public int AdvanceAllOneStep()
    {
        int moved = 0;

        // snapshot to avoid modifying while iterating
        var pairs = new List<(UiCard card, Zone from, Zone to)>();
        foreach (var z in Zones)
        {
            if (!z || z.IsEmpty || !z.nextZone) continue;
            foreach (var c in z.Occupants) pairs.Add((c, z, z.nextZone));
        }

        foreach (var (card, from, to) in pairs)
        {
            if (TryMoveCard(card, from, to, MoveType.System, out _))
            {
                card.AnimateToCurrentZone();
                moved++;
            }
        }
        return moved;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceEndTurn();
        }
    }

    [Button]
    // Example "end turn" hook: advance Deployment ¡÷ Advance, then Advance ¡÷ Combat
    public void AdvanceEndTurn()
    {
        AdvanceAllOneStep();
    }
}