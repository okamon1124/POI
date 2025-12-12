using UnityEngine;

/// <summary>
/// Handles card drops onto slots:
/// - Listens to SlotDropEvent (drag logic is elsewhere)
/// - Converts UiCard/UiSlot to CardInstance/Slot
/// - Calls Board.TryMoveCard
/// - If move is invalid, animates card back to model position
/// 
/// Uses CardInputPolicy to validate drops instead of tracking phase state internally.
/// Actual UI movement and animation is handled by CardMovementPresenter.
/// </summary>
public class CardDropInputHandler : MonoBehaviour
{
    private Board _board;
    private CardInputPolicy _inputPolicy;

    /// <summary>
    /// Initialize with board and input policy.
    /// </summary>
    public void Initialize(Board board, CardInputPolicy inputPolicy)
    {
        _board = board;
        _inputPolicy = inputPolicy;

        // Unsubscribe first to avoid duplicate subscriptions on re-initialize
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void Awake()
    {
        // Subscribe in Awake as fallback if Initialize isn't called
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
        EventBus.Subscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<SlotDropEvent>(OnSlotDrop);
    }

    private void OnSlotDrop(SlotDropEvent e)
    {
        if (_board == null)
        {
            Debug.LogWarning("[CardDropInputHandler] Board is null.");
            return;
        }

        // Validate drop using the input policy
        if (!ValidateDrop(e))
        {
            e.Card?.AnimateToCurrentZone();
            return;
        }

        var uiSlot = e.Slot;
        var uiCard = e.Card;

        if (uiSlot == null || uiCard == null)
            return;

        var instance = uiCard.cardInstance;
        var fromSlot = instance?.CurrentSlot;
        var toSlot = uiSlot.ModelSlot;

        if (instance == null || fromSlot == null || toSlot == null)
            return;

        // Attempt the move through the model
        if (!_board.TryMoveCard(instance, fromSlot, toSlot, MoveType.Player, out var reason))
        {
            Debug.Log($"[CardDropInputHandler] Move rejected: {reason}");
            // Model unchanged - animate card back to current model position
            uiCard.AnimateToCurrentZone();
            return;
        }

        // Move successful - Board/Presenter handles the UI update
    }

    private bool ValidateDrop(SlotDropEvent e)
    {
        // If no policy, allow the drop (for testing/fallback)
        if (_inputPolicy == null)
        {
            Debug.LogWarning("[CardDropInputHandler] InputPolicy is null, allowing drop.");
            return true;
        }

        // Check if the drag was started in a valid phase and is still valid
        if (!_inputPolicy.IsCurrentDragValid)
        {
            Debug.Log("[CardDropInputHandler] Drop rejected: drag did not start in current Main phase.");
            return false;
        }

        return true;
    }
}