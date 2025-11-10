using UnityEngine;

public class PlayerControlState : Singleton<PlayerControlState>
{
    private int _hoverCount = 0;

    private UiCard _hovering = null;
    private UiCard _dragging = null;

    // ---- Public read-only facts
    public UiCard HoveringCard => _hovering;
    public UiCard DraggingCard => _dragging;

    // ---- Derived convenience
    public bool IsHovering => _hovering != null;
    public bool IsDragging => _dragging != null;
    public bool IsHoveringWhileDrag => IsDragging && IsHovering && _hovering != _dragging;

    private void OnEnable()
    {
        EventBus.Subscribe<CardHoverEnterEvent>(OnHoverEnter);
        EventBus.Subscribe<CardHoverExitEvent>(OnHoverExit);
        EventBus.Subscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Subscribe<CardDragEndEvent>(OnDragEnd);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CardHoverEnterEvent>(OnHoverEnter);
        EventBus.Unsubscribe<CardHoverExitEvent>(OnHoverExit);
        EventBus.Unsubscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Unsubscribe<CardDragEndEvent>(OnDragEnd);
    }

    private void OnHoverEnter(CardHoverEnterEvent e)
    {
        _hoverCount++;

        // prefer most-recent card as focused hover
        if (_hovering != e.Card)
        {
            _hovering = e.Card;
            EventBus.Publish(new PlayerHoverChangedEvent(_hovering));
        }
    }

    private void OnHoverExit(CardHoverExitEvent e)
    {
        _hoverCount = Mathf.Max(0, _hoverCount - 1);

        if (_hovering == e.Card)
        {
            _hovering = null;

            // Only announce "no hover" if truly none left
            if (_hoverCount == 0)
                EventBus.Publish(new PlayerHoverChangedEvent(null));
        }
        // If another card is still hovered, its Enter already set focus.
    }

    private void OnDragBegin(CardDragBeginEvent e)
    {
        if (_dragging != e.Card)
        {
            _dragging = e.Card;
            EventBus.Publish(new PlayerDragChangedEvent(_dragging));
        }
    }

    private void OnDragEnd(CardDragEndEvent e)
    {
        if (_dragging == e.Card)
        {
            _dragging = null;
            EventBus.Publish(new PlayerDragChangedEvent(null));
        }
    }

    public bool IsAnotherCardBeingDragged(UiCard card)
    {
        bool anotherDragging =
        IsValid() &&
        IsDragging &&
        DraggingCard != card;

        return anotherDragging;
    }
}