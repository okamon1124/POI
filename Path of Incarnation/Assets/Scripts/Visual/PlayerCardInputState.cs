using UnityEngine;

public class PlayerCardInputState : Singleton<PlayerCardInputState>
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

        // Only clear _hovering if this was the current hovered card
        if (_hovering == e.Card)
        {
            // Don't immediately set to null - wait one frame to see if we're entering another card
            StartCoroutine(DelayedHoverClear());
        }
    }

    private System.Collections.IEnumerator DelayedHoverClear()
    {
        // Store the current hovering card
        UiCard previouslyHovering = _hovering;

        // Wait one frame
        yield return null;

        // If we're still not hovering over anything new, clear it
        if (_hovering == previouslyHovering && _hoverCount == 0)
        {
            _hovering = null;
            EventBus.Publish(new PlayerHoverChangedEvent(null));
        }
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