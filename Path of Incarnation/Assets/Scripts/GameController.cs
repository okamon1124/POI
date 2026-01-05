using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main game controller that manages initialization, phase flow, and game state.
/// Integrates PhaseManager with existing presenter/input handler systems.
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("UI Services")]
    [SerializeField] private UiRegistry uiRegistry;

    [Header("Presenters (Model -> View)")]
    [SerializeField] private CardDrawPresenter cardDrawPresenter;
    [SerializeField] private BoardCardSpawnPresenter boardCardSpawnPresenter;
    [SerializeField] private CardAvailabilityPresenter cardAvailabilityPresenter;
    [SerializeField] private SlotHighlightPresenter slotHighlightPresenter;
    [SerializeField] private CardMovementPresenter cardMovementPresenter;
    [SerializeField] private CombatPresenter combatPresenter;
    [SerializeField] private PhaseUIPresenter phaseUIPresenter;
    [SerializeField] private HandLayoutPresenter handLayoutPresenter;
    [SerializeField] private ManaUIPresenter manaUIPresenter;

    [Header("Input Handlers (View -> Model)")]
    [SerializeField] private CardDropInputHandler cardDropInputHandler;
    [SerializeField] private PhaseInputHandler phaseInputHandler;

    [Header("Board Setup")]
    [SerializeField] private UiZoneGroup[] boardZoneGroups;
    [SerializeField] private int playerHandCapacity = 10;
    [SerializeField] private int opponentHandCapacity = 10;

    [Header("Game Config")]
    [SerializeField] private DeckListData playerDeckData;
    [SerializeField] private int playerStartingHealth = 20;
    [SerializeField] private int enemyStartingHealth = 20;

    [Header("Mana Config")]
    [SerializeField] private int startingMaxMana = 1;
    [SerializeField] private int absoluteMaxMana = 8;

    [Header("Phase Timing")]
    [SerializeField] private float movementAnimationDuration = 0.5f;
    [SerializeField] private float combatAnimationDuration = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private CardData debugEnemyCardData;
    [SerializeField] private CardData debugRangedCardData;

    // Core Systems
    public Board Board { get; private set; }
    public PhaseManager PhaseManager { get; private set; }
    public ManaSystem PlayerManaSystem { get; private set; }

    // Services
    public CardInputPolicy InputPolicy { get; private set; }

    private Deck _playerDeck;
    private PlayerState _playerState;
    private PlayerState _enemyState;

    // ========== INITIALIZATION ==========

    private void Awake()
    {
        InitializeSystems();
    }

    private void Start()
    {
        if (autoStartGame)
        {
            StartGame();
        }
    }

    private void Update()
    {
        // Let phase manager update
        PhaseManager?.Update();

        // Debug hotkeys
        HandleDebugInput();
    }

    private void InitializeSystems()
    {
        // 1. Create model zones from scene + config
        var zones = CreateZones(out var zoneToGroup);

        // 2. Create board
        Board = new Board(zones);

        // 3. Create player states
        _playerState = new PlayerState(Owner.Player, playerStartingHealth);
        _enemyState = new PlayerState(Owner.Opponent, enemyStartingHealth);

        // 4. Create mana system
        PlayerManaSystem = new ManaSystem(Owner.Player, startingMaxMana, absoluteMaxMana);

        // 5. Inject mana system into MoveRules (for validation)
        MoveRules.PlayerManaSystem = PlayerManaSystem;
        MoveRules.OpponentManaSystem = null; // No opponent mana for PvE

        // 6. Create deck
        if (playerDeckData != null)
        {
            _playerDeck = new Deck(playerDeckData);
        }
        else
        {
            Debug.LogWarning("[GameController] No player deck data assigned!");
        }

        // 7. Create phase manager (with mana system)
        PhaseManager = new PhaseManager(Board, _playerState, _enemyState, _playerDeck, PlayerManaSystem);

        // Configure animation durations
        PhaseManager.MovementAnimationDuration = movementAnimationDuration;
        PhaseManager.CombatAnimationDuration = combatAnimationDuration;

        // 8. Create input policy (depends on PhaseManager)
        InputPolicy = new CardInputPolicy();
        InputPolicy.Initialize(PhaseManager);

        // 9. Subscribe to phase events
        PhaseManager.OnPhaseEntered += OnPhaseEntered;
        PhaseManager.OnPhaseExited += OnPhaseExited;
        PhaseManager.OnTurnCompleted += OnTurnCompleted;

        // 10. Subscribe to board events for mana spending
        Board.OnCardMoved += OnCardMovedForMana;

        // 11. Initialize presenters
        InitializePresenters();

        // 12. Initialize input handlers
        InitializeInputHandlers();

        // 13. Bind UI slots to model slots
        BindBoardSlots(zoneToGroup);
        ConfigureSlotPaths(zoneToGroup);

        //Debug.Log("[GameController] Systems initialized successfully.");
    }

    private void InitializePresenters()
    {
        cardDrawPresenter.Initialize(Board, uiRegistry, InputPolicy);
        boardCardSpawnPresenter.Initialize(Board, uiRegistry, InputPolicy);
        cardAvailabilityPresenter.Initialize(Board, uiRegistry, PhaseManager);
        slotHighlightPresenter.Initialize(Board, uiRegistry);
        cardMovementPresenter.Initialize(Board);
        combatPresenter.Initialize(Board, uiRegistry);

        // Hand layout presenter - auto-reflows when cards added/removed from hand
        if (handLayoutPresenter != null)
        {
            handLayoutPresenter.Initialize(Board);
        }

        // Mana UI presenter - syncs mana system with orb visuals
        if (manaUIPresenter != null)
        {
            manaUIPresenter.Initialize(PlayerManaSystem);
        }

        // Phase presenter needs PhaseManager, so initialize after it's created
        if (phaseUIPresenter != null)
        {
            phaseUIPresenter.Initialize(PhaseManager);
        }
    }

    private void InitializeInputHandlers()
    {
        // Pass InputPolicy instead of PhaseManager
        cardDropInputHandler.Initialize(Board, InputPolicy);

        // Phase input handler needs PhaseManager
        if (phaseInputHandler != null)
        {
            phaseInputHandler.Initialize(PhaseManager);
        }
    }

    // ========== ZONE SETUP ==========

    private List<Zone> CreateZones(out Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        var zones = new List<Zone>();
        zoneToGroup = new Dictionary<Zone, UiZoneGroup>();

        // Hand zones (no UiSlot needed - hand uses HandSplineLayout)
        zones.Add(new Zone(ZoneType.Hand, Owner.Player, playerHandCapacity));
        zones.Add(new Zone(ZoneType.Hand, Owner.Opponent, opponentHandCapacity));

        // Board zones (slots from scene UiZoneGroups)
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

            for (int i = 0; i < Mathf.Min(zone.Slots.Count, group.Slots.Length); i++)
            {
                var uiSlot = group.Slots[i];
                var modelSlot = zone.Slots[i];

                if (uiSlot == null) continue;

                uiSlot.Bind(modelSlot);
                uiRegistry.Register(uiSlot);
            }
        }
    }

    private void ConfigureSlotPaths(Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        foreach (var kv in zoneToGroup)
        {
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

    // ========== GAME FLOW ==========

    /// <summary>
    /// Start the game - begins with Draw phase.
    /// </summary>
    public void StartGame()
    {
        if (PhaseManager == null)
        {
            Debug.LogError("[GameController] Cannot start game - PhaseManager not initialized!");
            return;
        }

        //Debug.Log("[GameController] Starting game...");
        PhaseManager.StartGame();
    }

    /// <summary>
    /// Call this from "End Main Phase" UI button.
    /// </summary>
    public void OnEndMainPhaseButton()
    {
        PhaseManager?.EndMainPhase();
    }

    // ========== PHASE EVENT HANDLERS ==========

    private void OnPhaseEntered(PhaseType phase)
    {
        Debug.Log($"[GameController] === Entered {phase} Phase ===");

        switch (phase)
        {
            case PhaseType.Draw:
                OnDrawPhaseEntered();
                break;

            case PhaseType.Main:
                OnMainPhaseEntered();
                break;

            case PhaseType.Movement:
                OnMovementPhaseEntered();
                break;

            case PhaseType.Combat:
                OnCombatPhaseEntered();
                break;

            case PhaseType.EnemyTurn:
                OnEnemyTurnEntered();
                break;
        }
    }

    private void OnPhaseExited(PhaseType phase)
    {
        //Debug.Log($"[GameController] Exited {phase} Phase");

        if (phase == PhaseType.Main)
        {
            OnMainPhaseExited();
        }
    }

    private void OnTurnCompleted(int turnNumber)
    {
        Debug.Log($"[GameController] ═══ Turn {turnNumber} Completed ═══");
    }

    // ========== INDIVIDUAL PHASE HANDLERS ==========

    private void OnDrawPhaseEntered()
    {
        // Draw phase is automatic - just log
        //Debug.Log("[GameController] Drawing card...");
        // TODO: Play draw sound/animation
    }

    private void OnMainPhaseEntered()
    {
        //Debug.Log("[GameController] Main Phase - Player can play cards");
        // NOTE: Card drag permissions are now handled by CardInputPolicy
    }

    private void OnMainPhaseExited()
    {
        //Debug.Log("[GameController] Main Phase ended");
        // NOTE: Card drag permissions are now handled by CardInputPolicy
    }

    private void OnMovementPhaseEntered()
    {
        //Debug.Log("[GameController] Movement Phase - Creatures advancing...");
        // TODO: Play movement phase banner
        // Movement is automatic in PhaseManager
    }

    private void OnCombatPhaseEntered()
    {
        //Debug.Log("[GameController] Combat Phase - Resolving battles...");
        // TODO: Play combat phase banner
        // Combat is automatic in PhaseManager

        // Check game end conditions
        if (_playerState.Health <= 0)
        {
            OnGameEnd(false);
        }
        else if (_enemyState.Health <= 0)
        {
            OnGameEnd(true);
        }
    }

    private void OnEnemyTurnEntered()
    {
        //Debug.Log("[GameController] Enemy Turn (AI not implemented)");
        // TODO: Implement enemy AI
        // For now, enemy turn is a stub in PhaseManager
    }

    private void OnGameEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("[GameController] ★★★ VICTORY! ★★★");
            // TODO: Show victory screen
        }
        else
        {
            Debug.Log("[GameController] ☠ DEFEAT ☠");
            // TODO: Show defeat screen
        }
    }

    // ========== MANA SPENDING ==========

    private void OnCardMovedForMana(CardInstance card, Slot fromSlot, Slot toSlot)
    {
        // Spend mana when playing a card from hand
        if (fromSlot != null && fromSlot.Zone.Type == ZoneType.Hand)
        {
            BoardManaExtensions.SpendManaForCard(card, fromSlot, MoveType.Player);
        }
    }

    // ========== DEBUG UTILITIES ==========

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugDrawCard();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            DebugAdvanceAllCards();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            DebugSpawnEnemyCard();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            DebugForceCombat();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            DebugEndMainPhase();
        }
    }

    [Button("Draw Card (D)")]
    private void DebugDrawCard()
    {
        if (_playerDeck == null)
        {
            Debug.LogWarning("No deck configured!");
            return;
        }

        if (!_playerDeck.TryDraw(out var cardData))
        {
            Debug.LogWarning("Deck is empty!");
            return;
        }

        bool success = Board.TrySpawnCardToZone(
            cardData,
            ZoneType.Hand,
            Owner.Player,
            out var instance,
            out var reason
        );

        if (success)
        {
            Debug.Log($"Drew card: {cardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"Failed to draw: {reason}");
        }
    }

    [Button("Advance All (A)")]
    private void DebugAdvanceAllCards()
    {
        int count = Board.AdvanceAllOneStep();
        Debug.Log($"Advanced {count} creature(s)");
    }

    [Button("Spawn Enemy Card (E)")]
    private void DebugSpawnEnemyCard()
    {
        if (debugEnemyCardData == null)
        {
            Debug.LogWarning("No debug enemy card assigned!");
            return;
        }

        bool success = Board.TrySpawnCardToZone(
            debugEnemyCardData,
            ZoneType.Combat,
            Owner.Opponent,
            out var instance,
            out var reason
        );

        if (success)
        {
            Debug.Log($"Spawned enemy: {debugEnemyCardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"Failed to spawn enemy: {reason}");
        }
    }

    [Button("Force Combat (C)")]
    private void DebugForceCombat()
    {
        if (Board.MainCombatLane == null)
        {
            Debug.LogWarning("MainCombatLane is null!");
            return;
        }

        var result = Board.BeginMainCombat(_playerState, _enemyState, isPlayerTurn: true);

        if (result == null)
        {
            Debug.LogWarning("Combat returned null (no creatures in combat?)");
            return;
        }

        result.Apply(_playerState, _enemyState);

        Debug.Log($"Combat resolved! Player HP: {_playerState.Health}, Enemy HP: {_enemyState.Health}");

        foreach (var kv in result.CardDamages)
        {
            Debug.Log($"  {kv.Key.Owner}/{kv.Key.Data.cardName} took {kv.Value} damage -> HP: {kv.Key.CurrentHealth}");
        }
    }

    [Button("End Main Phase (Space)")]
    private void DebugEndMainPhase()
    {
        OnEndMainPhaseButton();
    }

    [Button("Start Game")]
    private void DebugStartGame()
    {
        StartGame();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PhaseManager != null)
        {
            PhaseManager.OnPhaseEntered -= OnPhaseEntered;
            PhaseManager.OnPhaseExited -= OnPhaseExited;
            PhaseManager.OnTurnCompleted -= OnTurnCompleted;
        }

        if (Board != null)
        {
            Board.OnCardMoved -= OnCardMovedForMana;
        }

        // Dispose input policy
        InputPolicy?.Dispose();
    }
}