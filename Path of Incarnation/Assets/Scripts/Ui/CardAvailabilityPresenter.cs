using UnityEngine;

/// <summary>
/// Presenter that updates card visual availability based on game state.
/// 
/// Responsibilities:
/// - Updates card interactable state (visual feedback like outline/glow)
/// - Responds to board changes and phase changes
/// 
/// Does NOT control drag permission directly - that's handled by:
/// - CardInputPolicy (phase-based permission)
/// - UiCard combines policy + interactable state for final drag decision
/// </summary>
public class CardAvailabilityPresenter : MonoBehaviour
{
    private Board _board;
    private UiRegistry _registry;
    private PhaseManager _phaseManager;

    public void Initialize(Board board, UiRegistry uiRegistry, PhaseManager phaseManager)
    {
        _board = board;
        _registry = uiRegistry;
        _phaseManager = phaseManager;

        _board.OnCardMoved += OnCardMoved;
        _board.OnCardSpawned += OnCardSpawned;

        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered += OnPhaseEntered;
        }
    }

    private void OnDestroy()
    {
        if (_board != null)
        {
            _board.OnCardMoved -= OnCardMoved;
            _board.OnCardSpawned -= OnCardSpawned;
        }

        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered -= OnPhaseEntered;
        }
    }

    private void OnCardMoved(CardInstance card, Slot from, Slot to)
    {
        RefreshAvailability();
    }

    private void OnCardSpawned(CardInstance card, Slot slot)
    {
        RefreshAvailability();
    }

    private void OnPhaseEntered(PhaseType phase)
    {
        RefreshAvailability();
    }

    /// <summary>
    /// Refresh visual availability for all player cards.
    /// Sets interactable state based on whether the card has valid moves.
    /// </summary>
    private void RefreshAvailability()
    {
        // Only show cards as "available" during main phase
        bool isMainPhase = _phaseManager?.CurrentPhase == PhaseType.Main;

        foreach (var uiCard in _registry.GetUiCardsByOwner(Owner.Player))
        {
            var instance = uiCard.cardInstance;
            if (instance == null) continue;

            // Check if card has any valid destination
            bool hasValidMoves = _board.HasValidDestination(instance, MoveType.Player);

            // Card is interactable if it's main phase AND has valid moves
            // This affects visual feedback (outline, glow, etc.)
            // Actual drag permission is determined by UiCard using InputPolicy + this interactable state
            bool isAvailable = isMainPhase && hasValidMoves;

            uiCard.SetInteractable(isAvailable);
        }
    }
}