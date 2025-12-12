using UnityEngine;

/// <summary>
/// Presenter that keeps HandSplineLayout synchronized with the hand zone.
/// Reflows ONLY when:
/// - model says cards in/out of hand changed
/// - no active drags
/// - all movement animations finished (via UiCardSettledEvent)
/// Pattern: PRESENTER (Model -> View)
/// </summary>
public class HandLayoutPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandSplineLayout playerHandLayout;
    
    private UiRegistry uiRegistry;

    private Board _board;

    // How many cards are currently being dragged
    private int _activeDrags = 0;

    // How many cards are currently animating into/out of a hand zone
    private int _activeAnimations = 0;

    /// <summary>
    /// Initialize with Board to listen to card events.
    /// </summary>
    public void Initialize(Board board)
    {
        if (_board != null)
        {
            UnsubscribeFromBoard();
        }

        _board = board;

        if (_board != null)
        {
            SubscribeToBoard();
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Subscribe<CardDragEndEvent>(OnDragEnd);
        EventBus.Subscribe<UiCardSettledEvent>(OnUiCardSettled);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Unsubscribe<CardDragEndEvent>(OnDragEnd);
        EventBus.Unsubscribe<UiCardSettledEvent>(OnUiCardSettled);
    }

    private void OnDestroy()
    {
        UnsubscribeFromBoard();
    }

    // ========== BOARD SUBSCRIPTION ==========

    private void SubscribeToBoard()
    {
        _board.OnCardSpawned += OnCardSpawned;
        _board.OnCardMoved += OnCardMoved;
    }

    private void UnsubscribeFromBoard()
    {
        if (_board != null)
        {
            _board.OnCardSpawned -= OnCardSpawned;
            _board.OnCardMoved -= OnCardMoved;
        }
    }

    // ========== DRAG EVENTS ==========

    private void OnDragBegin(CardDragBeginEvent e)
    {
        _activeDrags++;
    }

    private void OnDragEnd(CardDragEndEvent e)
    {
        _activeDrags = Mathf.Max(0, _activeDrags - 1);

        // Don't reflow here - wait for either:
        // - UiCardSettledEvent (if card moved to new zone)
        // - Or add a small delay to let the drop resolve first

        // Remove this line:
        // TryReflow();
    }

    // ========== CARD ANIMATION EVENTS ==========

    private void OnUiCardSettled(UiCardSettledEvent e)
    {
        // A card finished its movement tween (e.g. from hand to board or vice versa)
        _activeAnimations = Mathf.Max(0, _activeAnimations - 1);
        TryReflow();
    }

    // ========== BOARD EVENTS ==========

    private void OnCardSpawned(CardInstance card, Slot slot)
    {
        if (slot?.Zone == null) return;

        if (slot.Zone.Type == ZoneType.Hand)
        {
            RegisterAnimationIfUiExists(card);
        }
    }

    private void OnCardMoved(CardInstance card, Slot fromSlot, Slot toSlot)
    {
        bool leftHand = fromSlot != null && fromSlot.Zone != null && fromSlot.Zone.Type == ZoneType.Hand;
        bool enteredHand = toSlot != null && toSlot.Zone != null && toSlot.Zone.Type == ZoneType.Hand;

        if (leftHand || enteredHand)
        {
            RegisterAnimationIfUiExists(card);
        }
    }

    private void RegisterAnimationIfUiExists(CardInstance card)
    {
        if (uiRegistry == null) return;

        UiCard ui = uiRegistry.GetUiCard(card);
        if (ui != null)
        {
            // We expect an animation (AnimateToCurrentZone) that will eventually fire UiCardSettledEvent
            _activeAnimations++;
        }

        TryReflow();
    }

    // ========== REFLOW ==========

    private void TryReflow()
    {
        // Do NOT reflow while:
        // - any card is being dragged
        // - any card is still animating into/out of the hand
        if (_activeDrags > 0) return;
        if (_activeAnimations > 0) return;

        ReflowHandForOwner(Owner.Player);
    }

    private void ReflowHandForOwner(Owner owner)
    {
        if (owner != Owner.Player) return;
        if (playerHandLayout == null) return;

        playerHandLayout.NotifyCardsChanged();
        playerHandLayout.Reflow();
    }
}
