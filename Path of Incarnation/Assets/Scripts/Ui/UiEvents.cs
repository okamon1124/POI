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

public struct ZonePointerEnterEvent : IGameEvent
{
    public UiZone Zone;
    public ZonePointerEnterEvent(UiZone z) { Zone = z; }
}
public struct ZonePointerExitEvent : IGameEvent
{
    public UiZone Zone;
    public ZonePointerExitEvent(UiZone z) { Zone = z; }
}

public struct BoardStateChangedEvent : IGameEvent { }