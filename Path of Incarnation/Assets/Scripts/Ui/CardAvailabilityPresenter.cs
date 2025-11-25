using UnityEngine;

public class CardAvailabilityPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry registry;

    public void Initialize(Board boardController, UiRegistry uiRegistry)
    {
        board = boardController;
        registry = uiRegistry;

        board.OnCardMoved += OnBoardChanged;
        board.OnCardSpawned += OnBoardChanged;
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnCardMoved -= OnBoardChanged;
            board.OnCardSpawned -= OnBoardChanged;
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

    // ⭐ 不依賴 UI 層級，只看 UiRegistry
    private void RefreshAvailability()
    {
        foreach (var uiCard in registry.GetUiCardsByOwner(Owner.Player))
        {
            var instance = uiCard.cardInstance;
            if (instance == null) continue;

            bool canMove = board.HasValidDestination(instance, MoveType.Player);
            uiCard.SetInteractable(canMove);
        }
    }
}