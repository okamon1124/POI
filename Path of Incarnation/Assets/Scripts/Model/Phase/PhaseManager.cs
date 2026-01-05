using System;
using UnityEngine;

/// <summary>
/// Manages the game's phase flow and turn system.
/// Controls transitions between Draw, Main, Movement, and Combat phases.
/// </summary>
public class PhaseManager
{
    // ----------------- Events -----------------

    /// <summary>
    /// Fired when entering a new phase.
    /// UI can listen to this to show phase banners, enable/disable buttons, etc.
    /// </summary>
    public event Action<PhaseType> OnPhaseEntered;

    /// <summary>
    /// Fired when exiting a phase.
    /// </summary>
    public event Action<PhaseType> OnPhaseExited;

    /// <summary>
    /// Fired when a complete turn cycle finishes (after Combat phase).
    /// </summary>
    public event Action<int> OnTurnCompleted;

    // ----------------- State -----------------

    public PhaseType CurrentPhase { get; private set; }
    public int TurnNumber { get; private set; } = 1;
    public bool IsPlayerTurn => CurrentPhase != PhaseType.EnemyTurn;

    private readonly Board _board;
    private readonly PlayerState _playerState;
    private readonly PlayerState _enemyState;
    private readonly Deck _playerDeck;
    private readonly ManaSystem _playerManaSystem;

    private IPhase _currentPhaseHandler;

    // Animation timing configuration
    public float MovementAnimationDuration { get; set; } = 0.5f;
    public float CombatAnimationDuration { get; set; } = 1.0f;

    // ----------------- Constructor -----------------

    public PhaseManager(Board board, PlayerState playerState, PlayerState enemyState, Deck playerDeck, ManaSystem playerManaSystem = null)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        _enemyState = enemyState ?? throw new ArgumentNullException(nameof(enemyState));
        _playerDeck = playerDeck ?? throw new ArgumentNullException(nameof(playerDeck));
        _playerManaSystem = playerManaSystem; // Optional - game can work without mana
    }

    // ----------------- Public API -----------------

    /// <summary>
    /// Start the game by entering the first Draw phase.
    /// Call this once at game start.
    /// </summary>
    public void StartGame()
    {
        TurnNumber = 1;
        TransitionToPhase(PhaseType.Draw);
    }

    /// <summary>
    /// Call this when player presses "End Main Phase" button.
    /// Only valid during Main phase.
    /// </summary>
    public void EndMainPhase()
    {
        if (CurrentPhase != PhaseType.Main)
        {
            // Silently ignore if not in main phase
            // UI should prevent this, but check just in case
            return;
        }

        TransitionToPhase(PhaseType.Movement);
    }

    /// <summary>
    /// Update is called each frame. Allows phases to run their logic.
    /// Call this from your game controller's Update().
    /// </summary>
    public void Update()
    {
        _currentPhaseHandler?.OnUpdate();
    }

    // ----------------- Internal Phase Transitions -----------------

    private void TransitionToPhase(PhaseType nextPhase)
    {
        // Exit current phase
        if (_currentPhaseHandler != null)
        {
            _currentPhaseHandler.OnExit();
            OnPhaseExited?.Invoke(CurrentPhase);
        }

        // Update state
        PhaseType previousPhase = CurrentPhase;
        CurrentPhase = nextPhase;

        // Create new phase handler
        _currentPhaseHandler = CreatePhaseHandler(nextPhase);

        // Enter new phase
        OnPhaseEntered?.Invoke(nextPhase);
        _currentPhaseHandler?.OnEnter();

        Debug.Log($"[PhaseManager] Turn {TurnNumber}: {previousPhase} -> {nextPhase}");
    }

    private IPhase CreatePhaseHandler(PhaseType phaseType)
    {
        return phaseType switch
        {
            PhaseType.Draw => new DrawPhase(this, _board, _playerState, _playerDeck, _playerManaSystem),
            PhaseType.Main => new MainPhase(this),
            PhaseType.Movement => new MovementPhase(this, _board),
            PhaseType.Combat => new CombatPhase(this, _board, _playerState, _enemyState),
            PhaseType.EnemyTurn => new EnemyTurnPhase(this, _board, _playerState, _enemyState),  // Updated
            _ => throw new NotImplementedException($"Phase {phaseType} not implemented")
        };
    }

    /// <summary>
    /// Called by phase handlers to automatically progress to the next phase.
    /// </summary>
    internal void AdvanceToNextPhase()
    {
        PhaseType nextPhase = CurrentPhase switch
        {
            PhaseType.Draw => PhaseType.Main,
            PhaseType.Main => PhaseType.Movement,  // Should never happen (player triggers this manually)
            PhaseType.Movement => PhaseType.Combat,
            PhaseType.Combat => PhaseType.EnemyTurn,
            PhaseType.EnemyTurn => PhaseType.Draw,  // Loop back to player's draw
            _ => PhaseType.Draw
        };

        // If completing combat, increment turn and fire event
        if (CurrentPhase == PhaseType.Combat)
        {
            TurnNumber++;
            OnTurnCompleted?.Invoke(TurnNumber - 1);
        }

        TransitionToPhase(nextPhase);
    }
}