using UnityEngine;

public class CardDrawPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry uiRegistry;

    [Header("Prefabs & Roots")]
    [SerializeField] private UiCard cardPrefab;
    [SerializeField] private RectTransform cardsInHandRoot;
    [SerializeField] private RectTransform spawnPoint;   // where cards appear before tween

    public void Initialize(Board boardController, UiRegistry uiBoardRef)
    {
        if (board != null)
            board.OnCardSpawned -= OnCardSpawned;

        board = boardController;
        uiRegistry = uiBoardRef;

        if (board != null)
            board.OnCardSpawned += OnCardSpawned;
    }

    private void OnDestroy()
    {
        if (board != null)
            board.OnCardSpawned -= OnCardSpawned;
    }

    private void OnCardSpawned(CardInstance cardInstance, Slot slot)
    {
        // This presenter only cares about hand spawns
        if (slot.Zone.Type != ZoneType.Hand)
            return;

        // 1) Create UiCard under hand root
        var uiCard = Instantiate(cardPrefab, cardsInHandRoot);
        uiCard.cardInstance = cardInstance;
        uiCard.BindRegistry(uiRegistry);
        uiRegistry.Register(uiCard);

        // 2) Start it at spawnPoint
        if (spawnPoint != null)
        {
            var rt = uiCard.GetComponent<RectTransform>();
            Vector3 spawnWorld = spawnPoint.TransformPoint(spawnPoint.rect.center);

            Vector2 size = rt.rect.size;
            Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
            Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
            rt.position = spawnWorld - worldOffset;
        }

        // 3) Let UiCard / HandSplineLayout do the tween to its proper place
        uiCard.AnimateToCurrentZone();
        uiCard.RefreshVisual();
    }
}