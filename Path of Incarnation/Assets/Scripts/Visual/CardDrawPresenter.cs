using UnityEngine;

/// <summary>
/// Presenter that creates UiCard instances when cards are spawned into the hand.
/// Listens to Board.OnCardSpawned and creates the visual representation.
/// </summary>
public class CardDrawPresenter : MonoBehaviour
{
    private Board _board;
    private UiRegistry _uiRegistry;
    private CardInputPolicy _inputPolicy;

    [Header("Prefabs & Roots")]
    [SerializeField] private UiCard cardPrefab;
    [SerializeField] private RectTransform cardsInHandRoot;
    [SerializeField] private RectTransform spawnPoint;   // where cards appear before tween

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

    private void OnCardSpawned(CardInstance cardInstance, Slot slot)
    {
        // This presenter only handles hand spawns
        if (slot.Zone.Type != ZoneType.Hand)
            return;

        // 1) Create UiCard under hand root
        var uiCard = Instantiate(cardPrefab, cardsInHandRoot);
        uiCard.cardInstance = cardInstance;

        // 2) Bind dependencies
        uiCard.BindRegistry(_uiRegistry);
        uiCard.BindInputPolicy(_inputPolicy);
        _uiRegistry.Register(uiCard);

        // 3) Start it at spawnPoint
        if (spawnPoint != null)
        {
            var rt = uiCard.GetComponent<RectTransform>();
            Vector3 spawnWorld = spawnPoint.TransformPoint(spawnPoint.rect.center);
            Vector2 size = rt.rect.size;
            Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
            Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
            rt.position = spawnWorld - worldOffset;
        }

        // 4) Let UiCard / HandSplineLayout do the tween to its proper place
        uiCard.AnimateToCurrentZone();
        uiCard.RefreshVisual();

        EventBus.Publish(new CardDrawnEvent(uiCard));
    }
}