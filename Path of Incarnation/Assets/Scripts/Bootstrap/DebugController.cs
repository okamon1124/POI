using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Debug utilities for testing game mechanics.
/// Handles debug hotkeys and provides inspector buttons.
/// </summary>
public class DebugController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameBootstrapper bootstrapper;

    [Header("Debug Cards")]
    [SerializeField] private CardData debugEnemyCardData;
    [SerializeField] private CardData debugRangedCardData;

    private Board Board => bootstrapper?.Board;
    private PhaseManager PhaseManager => bootstrapper?.PhaseManager;

    private Deck _playerDeck;
    private PlayerState _playerState;
    private PlayerState _enemyState;

    /// <summary>
    /// Call this after bootstrapper is initialized to inject debug-only references.
    /// </summary>
    public void Initialize(Deck playerDeck, PlayerState playerState, PlayerState enemyState)
    {
        _playerDeck = playerDeck;
        _playerState = playerState;
        _enemyState = enemyState;
    }

    private void Update()
    {
        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        if (Board == null) return;

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
            Debug.LogWarning("[DebugController] No deck configured!");
            return;
        }

        if (!_playerDeck.TryDraw(out var cardData))
        {
            Debug.LogWarning("[DebugController] Deck is empty!");
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
            Debug.Log($"[DebugController] Drew card: {cardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"[DebugController] Failed to draw: {reason}");
        }
    }

    [Button("Advance All (A)")]
    private void DebugAdvanceAllCards()
    {
        if (Board == null) return;

        int count = Board.AdvanceAllOneStep();
        Debug.Log($"[DebugController] Advanced {count} creature(s)");
    }

    [Button("Spawn Enemy Card (E)")]
    private void DebugSpawnEnemyCard()
    {
        if (debugEnemyCardData == null)
        {
            Debug.LogWarning("[DebugController] No debug enemy card assigned!");
            return;
        }

        if (Board == null) return;

        bool success = Board.TrySpawnCardToZone(
            debugEnemyCardData,
            ZoneType.Combat,
            Owner.Opponent,
            out var instance,
            out var reason
        );

        if (success)
        {
            Debug.Log($"[DebugController] Spawned enemy: {debugEnemyCardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"[DebugController] Failed to spawn enemy: {reason}");
        }
    }

    [Button("Force Combat (C)")]
    private void DebugForceCombat()
    {
        if (Board == null || _playerState == null || _enemyState == null)
        {
            Debug.LogWarning("[DebugController] Missing references for combat!");
            return;
        }

        if (Board.MainCombatLane == null)
        {
            Debug.LogWarning("[DebugController] MainCombatLane is null!");
            return;
        }

        var result = Board.BeginMainCombat(_playerState, _enemyState, isPlayerTurn: true);

        if (result == null)
        {
            Debug.LogWarning("[DebugController] Combat returned null (no creatures in combat?)");
            return;
        }

        result.Apply(_playerState, _enemyState);

        Debug.Log($"[DebugController] Combat resolved! Player HP: {_playerState.Health}, Enemy HP: {_enemyState.Health}");

        foreach (var kv in result.CardDamages)
        {
            Debug.Log($"  {kv.Key.Owner}/{kv.Key.Data.cardName} took {kv.Value} damage -> HP: {kv.Key.CurrentHealth}");
        }
    }

    [Button("End Main Phase (Space)")]
    private void DebugEndMainPhase()
    {
        PhaseManager?.EndMainPhase();
    }

    [Button("Start Game")]
    private void DebugStartGame()
    {
        PhaseManager?.StartGame();
    }
}