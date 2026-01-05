using UnityEngine;

/// <summary>
/// Interface for all phase handlers.
/// Each phase can have enter, update, and exit logic.
/// </summary>
public interface IPhase
{
    void OnEnter();
    void OnUpdate();
    void OnExit();
}

// =====================================================================
// DRAW PHASE
// =====================================================================

/// <summary>
/// Draw Phase: Player draws 1 card from deck to hand.
/// Also refreshes mana and gains +1 max mana per turn.
/// Waits for draw animation before advancing to Main Phase.
/// </summary>
public class DrawPhase : IPhase
{
    private readonly PhaseManager _manager;
    private readonly Board _board;
    private readonly PlayerState _playerState;
    private readonly Deck _deck;
    private readonly ManaSystem _manaSystem;

    private float _animationTimer = 0f;
    private const float DRAW_ANIMATION_DURATION = 0.3f; // Wait for card to fly to hand
    private bool _hasDrawn = false;
    private bool _hasAdvanced = false;

    public DrawPhase(PhaseManager manager, Board board, PlayerState playerState, Deck deck, ManaSystem manaSystem = null)
    {
        _manager = manager;
        _board = board;
        _playerState = playerState;
        _deck = deck;
        _manaSystem = manaSystem;
    }

    public void OnEnter()
    {
        _animationTimer = 0f;
        _hasDrawn = false;
        _hasAdvanced = false;

        // Handle mana first
        HandleMana();

        // Then draw card
        DrawCard();
    }

    public void OnUpdate()
    {
        if (_hasAdvanced) return;

        _animationTimer += UnityEngine.Time.deltaTime;

        // Wait for draw animation to finish before advancing
        if (_animationTimer >= DRAW_ANIMATION_DURATION)
        {
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
        }
    }

    public void OnExit() { }

    private void HandleMana()
    {
        if (_manaSystem == null) return;

        // Refresh mana to current max first
        _manaSystem.RefreshMana();

        // Then gain +1 max mana for next turn (capped at absolute max)
        _manaSystem.GainMaxMana(1);
    }

    private void DrawCard()
    {
        if (_hasDrawn) return;
        _hasDrawn = true;

        if (!_deck.TryDraw(out CardData cardData))
        {
            Debug.LogWarning("[DrawPhase] No cards left in deck!");
            // TODO: Handle deck-out condition (lose game, fatigue damage, etc.)

            // Still advance to Main phase even if no cards to draw
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
            return;
        }

        bool success = _board.TrySpawnCardToZone(
            cardData,
            ZoneType.Hand,
            _playerState.Owner,
            out CardInstance drawnCard,
            out string reason
        );

        if (success)
        {
            Debug.Log($"[DrawPhase] Drew card: {cardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"[DrawPhase] Failed to draw card: {reason}");
            // TODO: Handle full hand (discard, burn card, etc.)
        }
    }
}

// =====================================================================
// MAIN PHASE
// =====================================================================

/// <summary>
/// Main Phase: Player can play cards from hand to board.
/// Phase ends when player manually presses "End Main Phase" button.
/// Does NOT auto-advance - requires PhaseManager.EndMainPhase() call.
/// </summary>
public class MainPhase : IPhase
{
    private readonly PhaseManager _manager;

    public MainPhase(PhaseManager manager)
    {
        _manager = manager;
    }

    public void OnEnter()
    {
        Debug.Log("[MainPhase] Player can now play cards. Press 'End Main Phase' when ready.");
        // TODO: Enable "End Main Phase" button in UI
        // TODO: Enable card dragging/playing
    }

    public void OnUpdate()
    {
        // Player actions happen here via input system
        // Phase does NOT auto-advance - player must press button
    }

    public void OnExit()
    {
        Debug.Log("[MainPhase] Main phase ended.");
        // TODO: Disable "End Main Phase" button
        // TODO: Disable card dragging if needed
    }
}

// =====================================================================
// MOVEMENT PHASE
// =====================================================================

/// <summary>
/// Movement Phase: All creatures advance 1 step along their paths.
/// Waits for movement animations to finish before advancing to Combat Phase.
/// </summary>
public class MovementPhase : IPhase
{
    private readonly PhaseManager _manager;
    private readonly Board _board;

    private float _animationTimer = 0f;
    private bool _hasAdvanced = false;

    public MovementPhase(PhaseManager manager, Board board)
    {
        _manager = manager;
        _board = board;
    }

    public void OnEnter()
    {
        Debug.Log("[MovementPhase] Advancing all creatures...");

        int movedCount = _board.AdvanceAllOneStep();

        Debug.Log($"[MovementPhase] {movedCount} creature(s) moved.");

        _animationTimer = 0f;
        _hasAdvanced = false;

        // If no cards moved, advance immediately - no need to wait
        if (movedCount == 0)
        {
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
        }
        // If cards moved, wait for animation to complete
    }

    public void OnUpdate()
    {
        if (_hasAdvanced) return;

        _animationTimer += UnityEngine.Time.deltaTime;

        if (_animationTimer >= _manager.MovementAnimationDuration)
        {
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
        }
    }

    public void OnExit() { }
}

// =====================================================================
// COMBAT PHASE
// =====================================================================

/// <summary>
/// Combat Phase: Creatures in combat zones fight.
/// Waits for combat animations before advancing to next phase.
/// </summary>
public class CombatPhase : IPhase
{
    private readonly PhaseManager _manager;
    private readonly Board _board;
    private readonly PlayerState _playerState;
    private readonly PlayerState _enemyState;

    private float _animationTimer = 0f;
    private bool _hasAdvanced = false;

    public CombatPhase(PhaseManager manager, Board board, PlayerState playerState, PlayerState enemyState)
    {
        _manager = manager;
        _board = board;
        _playerState = playerState;
        _enemyState = enemyState;
    }

    public void OnEnter()
    {
        Debug.Log("[CombatPhase] Resolving combat...");

        // Trigger combat on the main combat lane
        CombatResult result = _board.BeginMainCombat(_playerState, _enemyState, isPlayerTurn: true);

        _animationTimer = 0f;
        _hasAdvanced = false;

        if (result != null)
        {
            Debug.Log($"[CombatPhase] Combat resolved. Player HP: {_playerState.Health}, Enemy HP: {_enemyState.Health}");
            // Board.OnCombatBegin event will notify UI/Presenter to animate

            // Check win/lose conditions
            CheckGameEnd();

            // Wait for combat animations to play
        }
        else
        {
            Debug.Log("[CombatPhase] No combat occurred (no creatures in combat zone)");

            // No combat, skip immediately - don't wait
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
            return;
        }
    }

    public void OnUpdate()
    {
        if (_hasAdvanced) return;

        _animationTimer += UnityEngine.Time.deltaTime;

        if (_animationTimer >= _manager.CombatAnimationDuration)
        {
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
        }
    }

    public void OnExit() { }

    private void CheckGameEnd()
    {
        if (_playerState.Health <= 0)
        {
            Debug.Log("[CombatPhase] Player defeated!");
            // TODO: Trigger game over
        }

        if (_enemyState.Health <= 0)
        {
            Debug.Log("[CombatPhase] Enemy defeated! Victory!");
            // TODO: Trigger victory screen
        }
    }
}

// =====================================================================
// ENEMY TURN PHASE (Stub for future AI)
// =====================================================================

/// <summary>
/// Enemy Turn: Placeholder for future AI implementation.
/// Currently just waits briefly then advances back to player's Draw phase.
/// </summary>
public class EnemyTurnPhase : IPhase
{
    private readonly PhaseManager _manager;
    private readonly Board _board;
    private readonly PlayerState _playerState;
    private readonly PlayerState _enemyState;

    private float _timer = 0f;
    private float _totalDuration = 0f;
    private bool _hasAdvanced = false;

    private const float TURN_TRANSITION_DELAY = 0.2f;
    private const float COMBAT_ANIMATION_DURATION = 1.0f;

    public EnemyTurnPhase(PhaseManager manager, Board board, PlayerState playerState, PlayerState enemyState)
    {
        _manager = manager;
        _board = board;
        _playerState = playerState;
        _enemyState = enemyState;
    }

    public void OnEnter()
    {
        Debug.Log("[EnemyTurn] Enemy turn starting...");

        _timer = 0f;
        _hasAdvanced = false;
        _totalDuration = TURN_TRANSITION_DELAY;

        // TODO: Enemy draws card
        // TODO: Enemy plays cards
        // TODO: Enemy creatures advance (call _board.AdvanceAllOneStep() for enemy side)

        // Enemy combat - enemy attacks
        CombatResult result = _board.BeginMainCombat(_playerState, _enemyState, isPlayerTurn: false);

        if (result != null)
        {
            Debug.Log($"[EnemyTurn] Enemy combat resolved. Player HP: {_playerState.Health}, Enemy HP: {_enemyState.Health}");
            _totalDuration = COMBAT_ANIMATION_DURATION;

            CheckGameEnd();
        }
        else
        {
            Debug.Log("[EnemyTurn] No enemy combat (no enemy creature in combat zone)");
        }
    }

    public void OnUpdate()
    {
        if (_hasAdvanced) return;

        _timer += UnityEngine.Time.deltaTime;

        if (_timer >= _totalDuration)
        {
            _manager.AdvanceToNextPhase();
            _hasAdvanced = true;
        }
    }

    public void OnExit() { }

    private void CheckGameEnd()
    {
        if (_playerState.Health <= 0)
        {
            Debug.Log("[EnemyTurn] Player defeated!");
        }

        if (_enemyState.Health <= 0)
        {
            Debug.Log("[EnemyTurn] Enemy defeated! Victory!");
        }
    }
}