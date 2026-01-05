using Cysharp.Threading.Tasks;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Simple test script to verify the Effect system works.
/// Attach to any GameObject and assign the required references.
/// 
/// Usage:
/// 1. Create a DrawCardsEffect asset (Right-click ¡÷ Create ¡÷ Effects ¡÷ SubEffects ¡÷ Draw Cards)
/// 2. Create an Effect asset (Right-click ¡÷ Create ¡÷ Effects ¡÷ Effect)
/// 3. Add one EffectStep to the Effect with your DrawCardsEffect
/// 4. Assign references in inspector
/// 5. Press Play and click "Test Draw Effect" button in inspector
/// </summary>
public class EffectSystemTest : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to your GameController to get Board, PlayerState, Deck")]
    [SerializeField] private GameBootstrapper gameController;

    [Header("Test Effects")]
    [SerializeField] private Effect drawEffect;
    [SerializeField] private Effect damageEffect;

    private EffectResolver _resolver;

    private void Awake()
    {
        _resolver = new EffectResolver();

        // Set up manual targeting handler (for future use)
        _resolver.OnManualTargetSelection = HandleManualTargeting;
    }

    // ========================= Test Buttons =========================

    [ContextMenu("Test Draw Effect")]
    [Button]
    public void TestDrawEffect()
    {
        if (drawEffect == null)
        {
            Debug.LogError("[EffectSystemTest] No draw effect assigned!");
            return;
        }

        TestEffectAsync(drawEffect).Forget();
    }

    [ContextMenu("Test Damage Effect")]
    [Button]
    public void TestDamageEffect()
    {
        if (damageEffect == null)
        {
            Debug.LogError("[EffectSystemTest] No damage effect assigned!");
            return;
        }

        TestEffectAsync(damageEffect).Forget();
    }

    [ContextMenu("Test Context Creation")]
    [Button]
    public void TestContextCreation()
    {
        TestSimpleDrawAsync();
    }

    // ========================= Test Implementation =========================

    private async UniTaskVoid TestEffectAsync(Effect effect)
    {
        if (gameController == null)
        {
            Debug.LogError("[EffectSystemTest] GameController not assigned!");
            return;
        }

        var context = CreateContext();
        if (context == null)
        {
            Debug.LogError("[EffectSystemTest] Failed to create context!");
            return;
        }

        Debug.Log($"[EffectSystemTest] === Testing Effect: {effect.EffectName} ===");

        bool success = await _resolver.Execute(effect, context);

        Debug.Log($"[EffectSystemTest] === Result: {(success ? "SUCCESS" : "FAILED")} ===");
    }

    /// <summary>
    /// Test drawing cards without needing to create ScriptableObjects.
    /// Useful for quick verification.
    /// </summary>
    private void TestSimpleDrawAsync()
    {
        if (gameController == null)
        {
            Debug.LogError("[EffectSystemTest] GameController not assigned!");
            return;
        }

        Debug.Log("[EffectSystemTest] === Testing Context Creation ===");

        var context = CreateContext();
        if (context == null)
        {
            Debug.LogError("[EffectSystemTest] Failed to create context!");
            return;
        }

        // For now, just test the context creation
        Debug.Log($"[EffectSystemTest] Context created successfully:");
        Debug.Log($"  - Owner: {context.Owner}");
        Debug.Log($"  - Board: {(context.Board != null ? "OK" : "NULL")}");
        Debug.Log($"  - OwnerDeck: {(context.OwnerDeck != null ? $"OK ({context.OwnerDeck.Count} cards)" : "NULL")}");
        Debug.Log($"  - OwnerState: {(context.OwnerState != null ? $"OK (HP: {context.OwnerState.Health})" : "NULL")}");
        Debug.Log($"  - OpponentState: {(context.OpponentState != null ? $"OK (HP: {context.OpponentState.Health})" : "NULL")}");

        Debug.Log("[EffectSystemTest] === Context Test Complete ===");
    }

    // ========================= Context Creation =========================

    private EffectContext CreateContext()
    {
        if (gameController == null) return null;

        // Access private fields via reflection or make them accessible
        // For now, assuming you'll add public getters or we use the existing public properties

        var board = gameController.Board;
        if (board == null)
        {
            Debug.LogError("[EffectSystemTest] Board is null!");
            return null;
        }

        // You'll need to expose these from GameController or pass them differently
        // For testing, let's assume you add these properties to GameController:
        //   public PlayerState PlayerState => _playerState;
        //   public PlayerState EnemyState => _enemyState;
        //   public Deck PlayerDeck => _playerDeck;

        // Placeholder - you need to modify GameController to expose these
        PlayerState playerState = null;
        PlayerState enemyState = null;
        Deck playerDeck = null;

        // Try to get via reflection (for testing only)
        var type = typeof(GameBootstrapper);

        var playerStateField = type.GetField("_playerState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerStateField != null)
            playerState = playerStateField.GetValue(gameController) as PlayerState;

        var enemyStateField = type.GetField("_enemyState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyStateField != null)
            enemyState = enemyStateField.GetValue(gameController) as PlayerState;

        var deckField = type.GetField("_playerDeck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (deckField != null)
            playerDeck = deckField.GetValue(gameController) as Deck;

        if (playerState == null || enemyState == null)
        {
            Debug.LogError("[EffectSystemTest] Could not get PlayerState/EnemyState from GameController!");
            return null;
        }

        return new EffectContext(
            source: null,  // No source card for this test
            owner: Owner.Player,
            board: board,
            ownerState: playerState,
            opponentState: enemyState,
            ownerDeck: playerDeck,
            opponentDeck: null  // No opponent deck in your current setup
        );
    }

    // ========================= Manual Targeting Handler =========================

    private UniTask<System.Collections.Generic.List<ITargetable>> HandleManualTargeting(
        EffectStep step,
        System.Collections.Generic.List<ITargetable> validTargets,
        int requiredCount,
        EffectContext context)
    {
        Debug.Log($"[EffectSystemTest] Manual targeting requested!");
        Debug.Log($"  - Valid targets: {validTargets.Count}");
        Debug.Log($"  - Required count: {requiredCount}");

        // For testing, just auto-select the first valid target(s)
        var selected = new System.Collections.Generic.List<ITargetable>();
        for (int i = 0; i < Mathf.Min(requiredCount, validTargets.Count); i++)
        {
            selected.Add(validTargets[i]);
            Debug.Log($"  - Auto-selected: {validTargets[i].DisplayName}");
        }

        return UniTask.FromResult(selected);
    }
}