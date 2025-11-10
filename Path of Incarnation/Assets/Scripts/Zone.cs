using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Rendering.Universal;

public enum ZoneHighlightLevel { Off, Available, Hot }

public enum ZoneType { Hand, Main, Combat, Environment, Equipment, Deployment , Advance}

public enum Team { Both, Ally, Enemy }

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(RectTransform))]
public class Zone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Ownership")]
    public Team OwnerTeam = Team.Both;

    [Header("Occupancy")]
    [SerializeField] private List<UiCard> occupants = new();   // internal, never null
    [SerializeField] private int capacity = 1;

    [Header("Highlight")]
    [SerializeField] private Light2D highlightLight;
    [SerializeField, Range(0, 5)] private float offIntensity = 0f;
    [SerializeField, Range(0, 5)] private float availableIntensity = 1.2f;
    [SerializeField, Range(0, 5)] private float hotIntensity = 2.0f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private Tween _tween;

    [Header("Flow")]
    [SerializeField] public Zone nextZone;

    public ZoneType zoneType = ZoneType.Hand;

    // Read-only exposure
    public IReadOnlyList<UiCard> Occupants => occupants;
    public bool IsEmpty => occupants.Count == 0;
    public bool IsFull => occupants.Count >= capacity;

    public event System.Action<Zone> OccupantsChanged;

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
        if (occupants.Contains(card)) return true;
        occupants.Add(card);
        OccupantsChanged?.Invoke(this);
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

    public bool Remove(UiCard card)
    {
        bool removed = card != null && occupants.Remove(card);
        if (removed) OccupantsChanged?.Invoke(this);
        return removed;
    }

    public void SetHighlightLevel(ZoneHighlightLevel level)
    {
        if (!highlightLight) return;

        float target = level switch
        {
            ZoneHighlightLevel.Off => offIntensity,
            ZoneHighlightLevel.Available => availableIntensity,
            ZoneHighlightLevel.Hot => hotIntensity,
            _ => offIntensity
        };

        _tween?.Kill();
        _tween = DOTween.To(
            () => highlightLight.intensity,
            v => highlightLight.intensity = v,
            target,
            fadeDuration
        ).SetEase(ease);
    }

    // --- Pointer events for highlighting logic ---
    public void OnPointerEnter(PointerEventData e)
    {
        //Debug.Log($"PointerEnter {zoneType}");
        EventBus.Publish(new ZonePointerEnterEvent(this));
    }

    public void OnPointerExit(PointerEventData e)
        => EventBus.Publish(new ZonePointerExitEvent(this));
}