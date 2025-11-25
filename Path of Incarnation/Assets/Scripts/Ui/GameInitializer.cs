using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("UI Services")]
    [SerializeField] private UiRegistry uiRegistry;

    [Header("Presenters (Model -> View)")]
    [SerializeField] private CardDrawPresenter cardDrawPresenter;
    [SerializeField] private BoardCardSpawnPresenter boardCardSpawnPresenter;
    [SerializeField] private CardAvailabilityPresenter cardAvailabilityPresenter;
    [SerializeField] private SlotHighlightPresenter slotHighlightPresenter;
    [SerializeField] private CardMovementPresenter cardMovementPresenter;
    [SerializeField] private CombatPresenter combbatPresenter;

    [Header("Input Handlers (View -> Model)")]
    [SerializeField] private CardDropInputHandler cardDropInputHandler;

    [Header("Board Zone Groups (NO Hand here)")]
    [SerializeField] private UiZoneGroup[] boardZoneGroups;

    [Header("Hand Config (model only)")]
    [SerializeField] private int playerHandCapacity = 10;
    [SerializeField] private bool createOpponentHand = false;
    [SerializeField] private int opponentHandCapacity = 10;

    [Header("Deck Config")]
    [SerializeField] private DeckListData playerDeckData;

    [Header("Debug")]
    [SerializeField] private CardData debugCardData;
    [SerializeField] private CardData debugRangedCardData;

    public Board Board { get; private set; }

    private Deck _playerDeck;

    [Header("Debug Combat Config")]
    [SerializeField] private int debugPlayerStartingHealth = 20;
    [SerializeField] private int debugEnemyStartingHealth = 20;

    private PlayerState debugPlayerState;
    private PlayerState debugEnemyState;

    private void Awake()
    {
        // 1) Build model zones
        var zones = CreateZonesFromSceneAndConfig(out var zoneToGroup);

        // 2) Create BoardController
        Board = new Board(zones);

        // 2.5) Deck
        if (playerDeckData != null)
            _playerDeck = new Deck(playerDeckData);

        // 3) Initialize
        cardDrawPresenter.Initialize(Board, uiRegistry);
        boardCardSpawnPresenter.Initialize(Board, uiRegistry);
        cardAvailabilityPresenter.Initialize(Board, uiRegistry);
        slotHighlightPresenter.Initialize(Board, uiRegistry);
        cardMovementPresenter.Initialize(Board);
        cardDropInputHandler.Initialize(Board);
        combbatPresenter.Initialize(Board, uiRegistry);

        // 4) Bind UiSlot -> Slot for board zones
        BindBoardSlots(zoneToGroup);
        ConfigureModelNextSlots(zoneToGroup);

        debugPlayerState = new PlayerState(Owner.Player, debugPlayerStartingHealth);
        debugEnemyState = new PlayerState(Owner.Opponent, debugEnemyStartingHealth);
    }

    private List<Zone> CreateZonesFromSceneAndConfig(
        out Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        var zones = new List<Zone>();
        zoneToGroup = new Dictionary<Zone, UiZoneGroup>();

        // --- HAND ZONES (no UiSlot needed) ---
        var playerHandZone = new Zone(ZoneType.Hand, Owner.Player, playerHandCapacity);
        zones.Add(playerHandZone);

        if (createOpponentHand)
        {
            var enemyHandZone = new Zone(ZoneType.Hand, Owner.Opponent, opponentHandCapacity);
            zones.Add(enemyHandZone);
        }

        // --- BOARD ZONES (slots come from scene UiZoneGroups) ---
        foreach (var group in boardZoneGroups)
        {
            if (group == null || group.Slots == null || group.Slots.Length == 0)
                continue;

            var zone = new Zone(group.zoneType, group.owner, group.Slots.Length);
            zones.Add(zone);
            zoneToGroup[zone] = group;
        }

        return zones;
    }

    private void BindBoardSlots(Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        foreach (var kv in zoneToGroup)
        {
            Zone zone = kv.Key;
            UiZoneGroup group = kv.Value;

            var modelSlots = zone.Slots;
            var uiSlots = group.Slots;

            int count = Mathf.Min(modelSlots.Count, uiSlots.Length);

            for (int i = 0; i < count; i++)
            {
                var uiSlot = uiSlots[i];
                var modelSlot = modelSlots[i];

                if (!uiSlot) continue;

                uiSlot.Bind(modelSlot);
                uiRegistry.Register(uiSlot);
            }
        }
    }

    private void ConfigureModelNextSlots(Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        foreach (var kv in zoneToGroup)
        {
            var zone = kv.Key;
            var group = kv.Value;

            foreach (var uiSlot in group.Slots)
            {
                if (uiSlot == null || uiSlot.ModelSlot == null)
                    continue;

                if (uiSlot.NextUiSlot != null)
                {
                    uiSlot.ModelSlot.NextSlot = uiSlot.NextUiSlot.ModelSlot;
                }
            }
        }
    }



    // 以下為暫時測試用
    public bool TryDrawCardToHand(out CardInstance instance, out string reason)
    {
        instance = null;

        Deck deck = _playerDeck;

        if (deck == null)
        {
            reason = "Deck not configured.";
            return false;
        }

        if (!deck.TryDraw(out var cardData))
        {
            reason = "Deck is empty.";
            return false;
        }

        // 用你原本 Board 的 API：從 data spawn 到 Hand
        return Board.TrySpawnCardToZone(
            cardData,
            ZoneType.Hand,
            Owner.Player,
            out instance,
            out reason);
    }

    public bool TrySpawnCardToEnemyCombat(out CardInstance instance, out string reason)
    {
        instance = null;

        // 用你原本 Board 的 API：從 data spawn 到 Hand
        return Board.TrySpawnCardToZone(
            debugCardData,
            ZoneType.Combat,
            Owner.Opponent,
            out instance,
            out reason);
    }

    public bool TrySpawnCardToEnemyAdvance(int index, out CardInstance instance, out string reason)
    {
        instance = null;

        // 用你原本 Board 的 API：從 data spawn 到 Hand
        return Board.TrySpawnCardToZone(
            debugRangedCardData,
            ZoneType.Advance,
            Owner.Opponent,
            out instance,
            out reason,
            index);
    }

    // Debug button (NaughtyAttributes)
    [Button]
    private void DebugDrawOneForPlayer()
    {
        if (!TryDrawCardToHand(out var instance, out var reason))
            Debug.LogWarning("Draw failed: " + reason);
        else
            Debug.Log($"Drew card {instance.Data.cardName} into hand.");
    }

    [Button]
    private void DebugAdvanceAllOneStep()
    {
        Board.AdvanceAllOneStep();
    }

    [Button]
    private void DebugSpawnEnemyCard()
    {
        if (!TrySpawnCardToEnemyCombat(out var instance, out var reason))
            Debug.LogWarning("Spawn failed: " + reason);
        else
            Debug.Log($"Spawn enemy card {instance.Data.cardName}");
    }

    [Button]
    private void DebugSpawnRangedCard()
    {
        if (!TrySpawnCardToEnemyAdvance(3, out var instance, out var reason))
            Debug.LogWarning("Spawn failed: " + reason);
        else
            Debug.Log($"Spawn enemy card {instance.Data.cardName}");
    }

    [Button]
    private void DebugResolveCombatOnce()
    {
        if (Board == null)
        {
            Debug.LogError("[GameInitializer] Board is null.");
            return;
        }

        if (Board.MainCombatLane == null)
        {
            Debug.LogWarning("[GameInitializer] MainCombatLane is null.");
            return;
        }

        var lane = Board.MainCombatLane;

        var result = Board.BeginMainCombat(debugPlayerState, debugEnemyState);

        // 套用邏輯上的血量變化
        result.Apply(debugPlayerState, debugEnemyState);

        Debug.Log($"[CombatDebug] PlayerHP: {debugPlayerState.Health}, EnemyHP: {debugEnemyState.Health}");

        foreach (var kv in result.CardDamages)
        {
            var card = kv.Key;
            var dmg = kv.Value;
            Debug.Log($"[CombatDebug] Card [{card.Owner}] {card.Data.name} took {dmg} dmg, HP now = {card.CurrentHealth}");
        }

        // 額外：你也可以印視覺用的事件
        foreach (var ev in result.HitEvents)
        {
            string targetStr = ev.TargetCard != null
                ? $"{ev.TargetCard.Owner}/{ev.TargetCard.Data.name}"
                : ev.TargetType.ToString();

            Debug.Log($"[HitEvent] {ev.Source?.Data.name ?? "System"} -> {targetStr}: {ev.Amount}");
        }
    }

}
