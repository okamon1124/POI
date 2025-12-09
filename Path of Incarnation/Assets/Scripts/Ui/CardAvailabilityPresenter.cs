using UnityEngine;

public class CardAvailabilityPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry registry;
    private PhaseManager phaseManager;

    public void Initialize(Board boardController, UiRegistry uiRegistry, PhaseManager phaseManager)
    {
        board = boardController;
        registry = uiRegistry;
        this.phaseManager = phaseManager;

        board.OnCardMoved += OnBoardChanged;
        board.OnCardSpawned += OnBoardChanged;

        if (phaseManager != null)
        {
            phaseManager.OnPhaseEntered += OnPhaseEntered;
        }
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnCardMoved -= OnBoardChanged;
            board.OnCardSpawned -= OnBoardChanged;
        }

        if (phaseManager != null)
        {
            phaseManager.OnPhaseEntered -= OnPhaseEntered;
        }
    }

    private void OnBoardChanged(CardInstance card, Slot from, Slot to)
    {
        RefreshAvailability();
    }

    private void OnBoardChanged(CardInstance card, Slot slot)
    {
        RefreshAvailability();
    }

    private void OnPhaseEntered(PhaseType phase)
    {
        RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        bool isMainPhase = phaseManager?.CurrentPhase == PhaseType.Main;

        foreach (var uiCard in registry.GetUiCardsByOwner(Owner.Player))
        {
            var instance = uiCard.cardInstance;
            if (instance == null) continue;

            bool canMove = board.HasValidDestination(instance, MoveType.Player);

            // Interactable = has valid moves (affects outline/light)
            uiCard.SetInteractable(canMove);

            // CanDrag = main phase AND has valid moves (affects actual dragging)
            uiCard.SetCanDrag(isMainPhase && canMove);
        }
    }
}