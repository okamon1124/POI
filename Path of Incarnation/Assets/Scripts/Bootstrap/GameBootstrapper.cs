using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure bootstrapper that wires up all game systems and dependencies.
/// Does not contain game logic - only initialization and dependency injection.
/// </summary>
public class GameBootstrapper : MonoBehaviour
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

    [Header("Startup")]
    [SerializeField] private bool autoStartGame = true;

    // Core Systems - exposed for other systems that need them
    public Board Board { get; private set; }
    public PhaseManager PhaseManager { get; private set; }
    public ManaSystem PlayerManaSystem { get; private set; }
    public CardInputPolicy InputPolicy { get; private set; }

    // Internal references
    private Deck _playerDeck;
    private PlayerState _playerState;
    private PlayerState _enemyState;
    private GameFlowController _flowController;

    private void Awake()
    {
        InitializeSystems();
    }

    private void Start()
    {
        if (autoStartGame)
        {
            _flowController.StartGame();
        }
    }

    private void Update()
    {
        PhaseManager?.Update();
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

        // 4. Create mana system (subscribes to Board events internally)
        PlayerManaSystem = new ManaSystem(Owner.Player, startingMaxMana, absoluteMaxMana);
        PlayerManaSystem.BindToBoard(Board);

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
            Debug.LogWarning("[GameBootstrapper] No player deck data assigned!");
        }

        // 7. Create phase manager
        PhaseManager = new PhaseManager(Board, _playerState, _enemyState, _playerDeck, PlayerManaSystem);
        PhaseManager.MovementAnimationDuration = movementAnimationDuration;
        PhaseManager.CombatAnimationDuration = combatAnimationDuration;

        // 8. Create input policy
        InputPolicy = new CardInputPolicy();
        InputPolicy.Initialize(PhaseManager);

        // 9. Create flow controller (handles phase events, game state)
        _flowController = new GameFlowController(PhaseManager, _playerState, _enemyState);

        // 10. Initialize presenters
        InitializePresenters();

        // 11. Initialize input handlers
        InitializeInputHandlers();

        // 12. Bind UI slots to model slots
        BindBoardSlots(zoneToGroup);
        ConfigureSlotPaths(zoneToGroup);
    }

    private void InitializePresenters()
    {
        cardDrawPresenter.Initialize(Board, uiRegistry, InputPolicy);
        boardCardSpawnPresenter.Initialize(Board, uiRegistry, InputPolicy);
        cardAvailabilityPresenter.Initialize(Board, uiRegistry, PhaseManager);
        slotHighlightPresenter.Initialize(Board, uiRegistry);
        cardMovementPresenter.Initialize(Board);
        combatPresenter.Initialize(Board, uiRegistry);

        if (handLayoutPresenter != null)
        {
            handLayoutPresenter.Initialize(Board);
        }

        if (manaUIPresenter != null)
        {
            manaUIPresenter.Initialize(PlayerManaSystem);
        }

        if (phaseUIPresenter != null)
        {
            phaseUIPresenter.Initialize(PhaseManager);
        }
    }

    private void InitializeInputHandlers()
    {
        cardDropInputHandler.Initialize(Board, InputPolicy);

        if (phaseInputHandler != null)
        {
            phaseInputHandler.Initialize(PhaseManager);
        }
    }

    private List<Zone> CreateZones(out Dictionary<Zone, UiZoneGroup> zoneToGroup)
    {
        var zones = new List<Zone>();
        zoneToGroup = new Dictionary<Zone, UiZoneGroup>();

        // Hand zones
        zones.Add(new Zone(ZoneType.Hand, Owner.Player, playerHandCapacity));
        zones.Add(new Zone(ZoneType.Hand, Owner.Opponent, opponentHandCapacity));

        // Board zones from scene
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

    private void OnDestroy()
    {
        _flowController?.Dispose();
        InputPolicy?.Dispose();
        PlayerManaSystem?.UnbindFromBoard(Board);
    }
}