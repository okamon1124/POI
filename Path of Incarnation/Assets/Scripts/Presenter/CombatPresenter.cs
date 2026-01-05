using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatPresenter : MonoBehaviour
{
    private Board board;
    private UiRegistry uiRegistry;

    // Store pending hits to play when impact event fires
    private readonly Queue<HitEvent> pendingHits = new();
    private bool isProcessingCombat = false;

    public void Initialize(Board board, UiRegistry registry)
    {
        if (this.board != null)
            this.board.OnCombatBegin -= HandleCombatBegin;

        this.board = board;
        this.uiRegistry = uiRegistry = registry;

        if (this.board != null)
            this.board.OnCombatBegin += HandleCombatBegin;

        // Subscribe to impact event for hit timing
        EventBus.Subscribe<CombatImpactEvent>(OnCombatImpact);
    }

    private void OnDestroy()
    {
        if (board != null)
            board.OnCombatBegin -= HandleCombatBegin;

        EventBus.Unsubscribe<CombatImpactEvent>(OnCombatImpact);
    }

    private void HandleCombatBegin(CombatResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[CombatPresenter] HandleCombatBegin called with null result.");
            return;
        }

        Debug.Log($"[CombatPresenter] OnCombatBegin received. " +
                  $"DamageToPlayer={result.DamageToPlayer}, DamageToEnemy={result.DamageToEnemy}, " +
                  $"CardDamagesCount={result.CardDamages.Count}, HitEventsCount={result.HitEvents.Count}");

        StartCoroutine(PlayMainCombatSequence(result));
    }

    private IEnumerator PlayMainCombatSequence(CombatResult result)
    {
        if (board == null)
        {
            Debug.LogError("[CombatPresenter] board is null.");
            yield break;
        }

        if (board.MainCombatLane == null)
        {
            Debug.LogWarning("[CombatPresenter] MainCombatLane is null.");
            yield break;
        }

        // No hits = no combat animation needed
        if (result.HitEvents.Count == 0)
        {
            Debug.Log("[CombatPresenter] No hit events. Skipping combat animation.");
            yield break;
        }

        isProcessingCombat = true;

        var lane = board.MainCombatLane;
        var playerCard = lane.PlayerCombatSlot.InSlotCardInstance;
        var enemyCard = lane.EnemyCombatSlot.InSlotCardInstance;

        UiCard playerUiCard = playerCard != null ? uiRegistry.GetUiCard(playerCard) : null;
        UiCard enemyUiCard = enemyCard != null ? uiRegistry.GetUiCard(enemyCard) : null;

        // Queue up hit events for when impact occurs
        pendingHits.Clear();
        foreach (var hit in result.HitEvents)
        {
            pendingHits.Enqueue(hit);
        }

        // Process each hit event - play attacker animation
        foreach (var hit in result.HitEvents)
        {
            if (hit.TargetType == HitTargetType.Enemy || hit.TargetType == HitTargetType.EnemyCard)
            {
                // Player is attacking
                if (playerUiCard != null)
                {
                    Debug.Log($"[CombatPresenter] Player attacks. Damage={hit.Amount}");
                    playerUiCard.PlayAttackImpact();
                }
            }
            else if (hit.TargetType == HitTargetType.Player || hit.TargetType == HitTargetType.PlayerCard)
            {
                // Enemy is attacking
                if (enemyUiCard != null)
                {
                    Debug.Log($"[CombatPresenter] Enemy attacks. Damage={hit.Amount}");
                    enemyUiCard.PlayAttackImpact();
                }
            }
        }

        yield return new WaitForSeconds(0.4f);

        isProcessingCombat = false;
        pendingHits.Clear();

        Debug.Log("[CombatPresenter] PlayMainCombatSequence end.");
    }

    /// <summary>
    /// Called when the attack animation reaches the impact point.
    /// This is when we play the hit feedback on targets.
    /// </summary>
    private void OnCombatImpact(CombatImpactEvent evt)
    {
        if (!isProcessingCombat || pendingHits.Count == 0)
            return;

        var hit = pendingHits.Dequeue();

        Debug.Log($"[CombatPresenter] Combat impact! isPlayer={evt.IsPlayer}, TargetType={hit.TargetType}");

        // Play hit feedback on the target
        if (hit.TargetType == HitTargetType.EnemyCard && hit.TargetCard != null)
        {
            var targetUiCard = uiRegistry.GetUiCard(hit.TargetCard);
            if (targetUiCard != null)
            {
                Debug.Log($"[CombatPresenter] Playing hit feedback on enemy card.");
                targetUiCard.PlayHitFeedback();
            }
        }
        else if (hit.TargetType == HitTargetType.PlayerCard && hit.TargetCard != null)
        {
            var targetUiCard = uiRegistry.GetUiCard(hit.TargetCard);
            if (targetUiCard != null)
            {
                Debug.Log($"[CombatPresenter] Playing hit feedback on player card.");
                targetUiCard.PlayHitFeedback();
            }
        }
        else if (hit.TargetType == HitTargetType.Enemy)
        {
            Debug.Log($"[CombatPresenter] Direct hit to enemy player! (TODO: play feedback on enemy portrait)");
            // TODO: Play feedback on enemy health bar / portrait
        }
        else if (hit.TargetType == HitTargetType.Player)
        {
            Debug.Log($"[CombatPresenter] Direct hit to player! (TODO: play feedback on player portrait)");
            // TODO: Play feedback on player health bar / portrait
        }
    }
}