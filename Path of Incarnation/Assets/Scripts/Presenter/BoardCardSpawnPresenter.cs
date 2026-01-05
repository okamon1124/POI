using UnityEngine;

/// <summary>
/// Presenter that creates UiCard instances when cards are spawned onto the board (non-hand zones).
/// Listens to Board.OnCardSpawned and creates the visual representation.
/// </summary>
public class BoardCardSpawnPresenter : MonoBehaviour
{
    private Board _board;
    private UiRegistry _uiRegistry;
    private CardInputPolicy _inputPolicy;

    [Header("Prefab & Parent")]
    [SerializeField] private UiCard cardPrefab;

    /// <summary>
    /// Parent for board cards (usually under Board UI root)
    /// </summary>
    [SerializeField] private RectTransform boardCardsRoot;

    public void Initialize(Board board, UiRegistry uiRegistry, CardInputPolicy inputPolicy)
    {
        // Unsubscribe from old board if re-initializing
        if (_board != null)
            _board.OnCardSpawned -= OnCardSpawned;

        _board = board;
        _uiRegistry = uiRegistry;
        _inputPolicy = inputPolicy;

        if (_board != null)
            _board.OnCardSpawned += OnCardSpawned;
    }

    private void OnDestroy()
    {
        if (_board != null)
            _board.OnCardSpawned -= OnCardSpawned;
    }

    private void OnCardSpawned(CardInstance instance, Slot slot)
    {
        // This presenter only handles non-hand spawns
        if (slot.Zone.Type == ZoneType.Hand)
            return;

        // 1) Find the corresponding UiSlot
        UiSlot uiSlot = _uiRegistry.GetUiSlot(slot);
        if (uiSlot == null)
        {
            Debug.LogWarning($"[BoardCardSpawnPresenter] No UiSlot found for Slot in Zone {slot.Zone.Type} index {slot.Index}");
            return;
        }

        // 2) Check if slot already has a card
        if (uiSlot.Occupants.Count > 0)
        {
            Debug.LogWarning($"[BoardCardSpawnPresenter] Slot {uiSlot.name} already contains a card.");
            return;
        }

        // 3) Instantiate the card
        UiCard uiCard = Instantiate(cardPrefab, boardCardsRoot);
        uiCard.cardInstance = instance;

        // 4) Bind dependencies
        uiCard.BindRegistry(_uiRegistry);
        uiCard.BindInputPolicy(_inputPolicy);
        _uiRegistry.Register(uiCard);

        // 5) Position card at slot (no animation, direct placement)
        var cardRT = uiCard.GetComponent<RectTransform>();
        var slotRT = uiSlot.RectTransform;
        cardRT.position = slotRT.position;
        cardRT.rotation = slotRT.rotation;
        cardRT.localScale = Vector3.one * 1.1f;

        // 6) Let slot track this UI card
        uiSlot.AttachCard(uiCard);

        // 7) Update card visuals
        uiCard.RefreshVisual();
    }
}