using System;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    // ----------------- Data -----------------

    private readonly List<Zone> _zones = new();
    public IReadOnlyList<Zone> Zones => _zones;

    /// <summary>
    /// Fired whenever a card successfully moves from one slot to another.
    /// </summary>
    public event Action<CardInstance, Slot, Slot> OnCardMoved;

    /// <summary>
    /// Fired when a card is spawned directly into a slot (e.g. Draw into Hand, create token).
    /// </summary>
    public event Action<CardInstance, Slot> OnCardSpawned;

    /// <summary>
    /// 當主戰鬥線要開打時觸發。
    /// Model 這邊先算出 CombatResult，再交給 Presenter 慢慢演出。
    /// </summary>
    public event Action<CombatResult> OnCombatBegin;

    /// <summary>
    /// 主戰鬥線（玩家 vs 敵人各一條路徑，1v1 戰鬥）。
    /// </summary>
    public CombatLane MainCombatLane { get; private set; }

    // ----------------- Ctor -----------------

    public Board(IEnumerable<Zone> zones)
    {
        if (zones != null)
            _zones.AddRange(zones);

        BuildMainCombatLane();
    }

    // ----------------- Zone helpers -----------------

    public Zone GetZone(ZoneType type, Owner owner)
    {
        return _zones.Find(z => z.Type == type && z.Owner == owner);
    }

    /// <summary>
    /// Convenience: all slots in all zones.
    /// </summary>
    public IEnumerable<Slot> GetAllSlots()
    {
        foreach (var z in _zones)
        {
            foreach (var s in z.Slots)
                yield return s;
        }
    }

    // ----------------- Combat lane building -----------------

    /// <summary>
    /// 建立主戰鬥線（目前假設只有一條：玩家 Deployment -> Advance -> Combat，
    /// 敵人 Deployment -> Advance -> Combat）。如果相關 zone 沒有在場景中設定，MainCombatLane 會是 null。
    /// </summary>
    private void BuildMainCombatLane()
    {
        // 嘗試抓出玩家/敵人各自的部署/前進/戰鬥區域
        var playerDeployment = GetZone(ZoneType.Deployment, Owner.Player);
        var playerAdvance = GetZone(ZoneType.Advance, Owner.Player);
        var playerCombat = GetZone(ZoneType.Combat, Owner.Player);

        var enemyDeployment = GetZone(ZoneType.Deployment, Owner.Opponent);
        var enemyAdvance = GetZone(ZoneType.Advance, Owner.Opponent);
        var enemyCombat = GetZone(ZoneType.Combat, Owner.Opponent);

        // 如果有任何一個沒設好，就先不建立 lane（避免遊戲一開始就 throw）
        if (playerDeployment == null || playerAdvance == null || playerCombat == null ||
            enemyDeployment == null || enemyAdvance == null || enemyCombat == null)
        {
            Debug.LogWarning("[BoardController] MainCombatLane could not be built. " +
                             "Make sure Deployment/Advance/Combat zones for both Player and Opponent exist.");
            MainCombatLane = null;
            return;
        }

        // 建立玩家這側路徑：Deployment -> Advance slots... -> Combat
        var playerPath = new List<Slot>();
        if (playerDeployment.Slots.Count > 0)
            playerPath.Add(playerDeployment.Slots[0]);

        playerPath.AddRange(playerAdvance.Slots);

        if (playerCombat.Slots.Count > 0)
            playerPath.Add(playerCombat.Slots[0]);

        // 建立敵人這側路徑：Deployment -> Advance slots... -> Combat
        var enemyPath = new List<Slot>();
        if (enemyDeployment.Slots.Count > 0)
            enemyPath.Add(enemyDeployment.Slots[0]);

        enemyPath.AddRange(enemyAdvance.Slots);

        if (enemyCombat.Slots.Count > 0)
            enemyPath.Add(enemyCombat.Slots[0]);

        if (playerPath.Count == 0 || enemyPath.Count == 0)
        {
            Debug.LogWarning("[BoardController] MainCombatLane paths are empty.");
            MainCombatLane = null;
            return;
        }

        MainCombatLane = new CombatLane(playerPath, enemyPath);
    }

    /// <summary>
    /// 外部呼叫：開始主戰鬥線。
    /// 這裡會先算出 CombatResult，丟 event，並把 result 回傳。
    /// </summary>
    public CombatResult BeginMainCombat(PlayerState playerState, PlayerState enemyState, bool isPlayerTurn)
    {
        if (MainCombatLane == null)
        {
            Debug.LogWarning("[Board] BeginMainCombat called but MainCombatLane is null.");
            return null;
        }

        var result = CombatSystem.Resolve(playerState, enemyState, MainCombatLane, isPlayerTurn);
        if (result == null)
            return null;

        OnCombatBegin?.Invoke(result);

        return result;
    }

    // ----------------- Spawning -----------------

    /// <summary>
    /// Create a new card instance and place it into the first empty slot
    /// of the given zone (e.g. Draw to Hand, create token in a lane).
    /// </summary>
    public bool TrySpawnCardToZone(
    CardData data,
    ZoneType zoneType,
    Owner owner,
    out CardInstance instance,
    out string reason,
    int? slotIndex = null)   // 新增：可選的 slotIndex
    {
        instance = null;

        if (data == null)
        {
            reason = "CardData is null.";
            return false;
        }

        var zone = GetZone(zoneType, owner);
        if (zone == null)
        {
            reason = $"Zone {zoneType} for {owner} not found.";
            return false;
        }

        Slot slot = null;

        // 有指定 index 的情況
        if (slotIndex.HasValue)
        {
            var idx = slotIndex.Value;

            if (idx < 0 || idx >= zone.Slots.Count)
            {
                reason = $"Slot index {idx} is out of range.";
                return false;
            }

            slot = zone.Slots[idx];

            if (!slot.IsEmpty)
            {
                reason = $"Slot index {idx} is not empty.";
                return false;
            }
        }
        else
        {
            // 沒有特別指定 -> 用原本的第一個空 slot
            slot = zone.GetFirstEmptySlot();
            if (slot == null)
            {
                reason = "Zone is full.";
                return false;
            }
        }

        var card = new CardInstance(data, owner);
        slot.PlaceCard(card);
        instance = card;

        OnCardSpawned?.Invoke(card, slot);

        reason = null;
        return true;
    }

    // ----------------- Movement -----------------

    public bool TryMoveCard(CardInstance card, Slot toSlot, MoveType moveType, out string reason)
    {
        var fromSlot = card?.CurrentSlot;
        return TryMoveCard(card, fromSlot, toSlot, moveType, out reason);
    }

    public bool TryMoveCard(CardInstance card, Slot fromSlot, Slot toSlot, MoveType moveType, out string reason)
    {
        // Logic / rules check (also validates nulls)
        if (!MoveRules.CanMove(card, fromSlot, toSlot, moveType, out reason))
            return false;

        // Commit move
        fromSlot?.RemoveCard();
        toSlot.PlaceCard(card);               // should update card.CurrentSlot

        OnCardMoved?.Invoke(card, fromSlot, toSlot);
        return true;
    }

    /// <summary>
    /// Does there exist at least one legal destination slot for this card?
    /// Useful for UI availability, etc.
    /// </summary>
    public bool HasValidDestination(CardInstance card, MoveType moveType)
    {
        var from = card?.CurrentSlot;
        if (from == null) return false;

        foreach (var zone in _zones)
        {
            foreach (var slot in zone.Slots)
            {
                if (slot == from) continue;
                if (!slot.IsEmpty && zone.Type != ZoneType.Hand) continue;

                if (MoveRules.CanMove(card, from, slot, moveType, out _))
                    return true;
            }
        }

        return false;
    }

    // ----------------- System / lane advancing -----------------

    /// <summary>
    /// Advance everyone exactly 1 step along their slot's NextSlot if allowed by rules.
    /// Used for system-driven lane progression (Deployment -> Advance -> Combat).
    /// </summary>
    public int AdvanceAllOneStep()
    {
        int moved = 0;
        var moves = new List<(CardInstance card, Slot from, Slot to)>();

        // Snapshot to avoid modifying while iterating
        foreach (var zone in _zones)
        {
            foreach (var slot in zone.Slots)
            {
                if (slot.InSlotCardInstance == null) continue;
                if (slot.NextSlot == null) continue;

                moves.Add((slot.InSlotCardInstance, slot, slot.NextSlot));
            }
        }

        foreach (var (card, from, to) in moves)
        {
            if (!MoveRules.CanMove(card, from, to, MoveType.System, out _))
                continue;

            from.RemoveCard();
            to.PlaceCard(card);
            OnCardMoved?.Invoke(card, from, to);
            moved++;
        }

        return moved;
    }
}