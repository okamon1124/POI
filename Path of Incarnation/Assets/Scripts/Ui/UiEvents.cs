// Card ¡÷ world
public struct CardHoverEnterEvent : IGameEvent { public UiCard Card; public CardHoverEnterEvent(UiCard c) { Card = c; } }
public struct CardHoverExitEvent : IGameEvent { public UiCard Card; public CardHoverExitEvent(UiCard c) { Card = c; } }
public struct CardDragBeginEvent : IGameEvent { public UiCard Card; public CardDragBeginEvent(UiCard c) { Card = c; } }
public struct CardDragEndEvent : IGameEvent { public UiCard Card; public CardDragEndEvent(UiCard c) { Card = c; } }

// PlayerState ¡÷ world (separate, targeted signals)
public struct PlayerHoverChangedEvent : IGameEvent
{
    public UiCard Hovered; // null when no card hovered
    public PlayerHoverChangedEvent(UiCard hovered) { Hovered = hovered; }
}
public struct PlayerDragChangedEvent : IGameEvent
{
    public UiCard Dragged; // null when not dragging
    public PlayerDragChangedEvent(UiCard dragged) { Dragged = dragged; }
}

public struct SlotPointerEnterEvent : IGameEvent
{
    public UiSlot Slot;
    public SlotPointerEnterEvent(UiSlot z) { Slot = z; }
}
public struct SlotPointerExitEvent : IGameEvent
{
    public UiSlot Slot;
    public SlotPointerExitEvent(UiSlot z) { Slot = z; }
}

public struct SlotDropEvent : IGameEvent
{
    public UiSlot Slot;
    public UiCard Card;

    public SlotDropEvent(UiSlot slot, UiCard card)
    {
        Slot = slot;
        Card = card;
    }
}

public struct BoardStateChangedEvent : IGameEvent { }

public struct UiCardSettledEvent : IGameEvent
{
    public UiCard Card;
    public UiCardSettledEvent(UiCard card) => Card = card;
}

/// <summary>
/// Fired by CombatPresenter at the moment of attack impact (for audio timing).
/// </summary>
public struct CombatImpactEvent : IGameEvent
{
    public bool IsPlayer;
    public CombatImpactEvent(bool isPlayer) => IsPlayer = isPlayer;
}

public struct CardDrawnEvent : IGameEvent
{
    public UiCard Card;
    public CardDrawnEvent(UiCard c) => Card = c;
}

/// <summary>
/// Fired when a slot becomes "Hot" (valid drop target while hovering during drag).
/// Published by SlotHighlightPresenter.
/// </summary>
public struct SlotHighlightHotEvent : IGameEvent
{
    public UiSlot Slot;
    public SlotHighlightHotEvent(UiSlot slot) => Slot = slot;
}

/// <summary>
/// Fired when player presses down on a card (before drag starts).
/// This is the moment the card snaps to mouse position.
/// Published by UiCard.OnPointerDown.
/// </summary>
public struct CardGrabbedEvent : IGameEvent
{
    public UiCard Card;
    public CardGrabbedEvent(UiCard c) => Card = c;
}