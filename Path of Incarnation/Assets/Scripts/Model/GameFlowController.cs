using System;
using UnityEngine;

/// <summary>
/// Handles game flow logic: phase transitions, win/lose conditions, turn management.
/// Plain C# class that subscribes to PhaseManager events.
/// </summary>
public class GameFlowController : IDisposable
{
    private readonly PhaseManager _phaseManager;
    private readonly PlayerState _playerState;
    private readonly PlayerState _enemyState;

    public GameFlowController(PhaseManager phaseManager, PlayerState playerState, PlayerState enemyState)
    {
        _phaseManager = phaseManager;
        _playerState = playerState;
        _enemyState = enemyState;

        _phaseManager.OnPhaseEntered += OnPhaseEntered;
        _phaseManager.OnPhaseExited += OnPhaseExited;
        _phaseManager.OnTurnCompleted += OnTurnCompleted;
    }

    public void StartGame()
    {
        if (_phaseManager == null)
        {
            Debug.LogError("[GameFlowController] Cannot start game - PhaseManager is null!");
            return;
        }

        _phaseManager.StartGame();
    }

    public void EndMainPhase()
    {
        _phaseManager?.EndMainPhase();
    }

    private void OnPhaseEntered(PhaseType phase)
    {
        Debug.Log($"[GameFlowController] === Entered {phase} Phase ===");

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
        if (phase == PhaseType.Main)
        {
            OnMainPhaseExited();
        }
    }

    private void OnTurnCompleted(int turnNumber)
    {
        Debug.Log($"[GameFlowController] ═══ Turn {turnNumber} Completed ═══");
    }

    private void OnDrawPhaseEntered()
    {
        // Draw phase is automatic
        // TODO: Play draw sound/animation
    }

    private void OnMainPhaseEntered()
    {
        // Card drag permissions handled by CardInputPolicy
    }

    private void OnMainPhaseExited()
    {
        // Card drag permissions handled by CardInputPolicy
    }

    private void OnMovementPhaseEntered()
    {
        // Movement is automatic in PhaseManager
        // TODO: Play movement phase banner
    }

    private void OnCombatPhaseEntered()
    {
        // Combat is automatic in PhaseManager
        // TODO: Play combat phase banner

        CheckGameEndConditions();
    }

    private void OnEnemyTurnEntered()
    {
        // TODO: Implement enemy AI
    }

    private void CheckGameEndConditions()
    {
        if (_playerState.Health <= 0)
        {
            OnGameEnd(playerWon: false);
        }
        else if (_enemyState.Health <= 0)
        {
            OnGameEnd(playerWon: true);
        }
    }

    private void OnGameEnd(bool playerWon)
    {
        if (playerWon)
        {
            Debug.Log("[GameFlowController] ★★★ VICTORY! ★★★");
            // TODO: Show victory screen
        }
        else
        {
            Debug.Log("[GameFlowController] ☠ DEFEAT ☠");
            // TODO: Show defeat screen
        }
    }

    public void Dispose()
    {
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered -= OnPhaseEntered;
            _phaseManager.OnPhaseExited -= OnPhaseExited;
            _phaseManager.OnTurnCompleted -= OnTurnCompleted;
        }
    }
}