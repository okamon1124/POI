using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum ZoneType { Hand, Main, Combat, Environment, Equipment, Deployment , Advance}

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(RectTransform))]
public class Zone : MonoBehaviour, IDropHandler
{
    // Side/owner, if you care later
    public bool IsEnemy;

    [Header("Occupancy")]
    [SerializeField] private List<UiCard> occupants = new();   // internal, never null
    [SerializeField] private int capacity = 1;

    [Header("Flow")]
    [SerializeField] public Zone nextZone;

    public ZoneType zoneType = ZoneType.Hand;

    // Read-only exposure
    public IReadOnlyList<UiCard> Occupants => occupants;
    public bool IsEmpty => occupants.Count == 0;
    public bool IsFull => occupants.Count >= capacity;

    private void OnEnable() => ZoneManager.I?.Register(this);
    private void OnDisable() => ZoneManager.I?.Unregister(this);

    private void Reset()
    {
        if (zoneType == ZoneType.Hand) capacity = 99;
    }

    private void OnValidate()
    {
        if (zoneType == ZoneType.Hand && capacity < 10) capacity = 99;
        if (occupants == null) occupants = new List<UiCard>();
    }

    public bool TryAdd(UiCard card)
    {
        if (card == null || IsFull) return false;
        if (occupants.Contains(card)) return true; // idempotent
        occupants.Add(card);
        return true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var cardGo = eventData.pointerDrag;
        var card = cardGo ? cardGo.GetComponent<UiCard>() : null;

        string reason;
        if (!MoveRules.CanMoveZoneToZone(card, card?.CurrentZone, this, MoveType.Player, out reason))
        {
            Debug.Log($"Drop rejected: {reason}");
            return;
        }

        Zone from = card.CurrentZone;
        Zone to = this;

        bool removed = from.Remove(card);
        bool added = to.TryAdd(card);

        if (removed && added)
        {
            card.AssignZone(to);
            Debug.Log($"Move success: {card.name} moved from {from.zoneType} ¡÷ {to.zoneType}");
        }
        else
        {
            if (removed) from.TryAdd(card);
            Debug.LogWarning($"Move failed or zone full: {card.name} stays in {from.zoneType}");
        }
    }

    public bool Remove(UiCard card) => card != null && occupants.Remove(card);
}