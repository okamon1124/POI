using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SubEffect that destroys target cards.
/// 
/// Example usage:
/// - "Destroy target creature"
/// - "Destroy all enemy creatures"
/// 
/// Writes destroyed cards to whiteboard if writeToKey is set,
/// allowing follow-up effects like "Destroy target creature, its owner draws a card".
/// </summary>
[CreateAssetMenu(fileName = "DestroyCardEffect", menuName = "Effects/SubEffects/Destroy Card")]
public class DestroyCardEffect : SubEffect
{
    public override async UniTask<bool> Execute(EffectContext context)
    {
        if (context.CurrentTargets.Count == 0)
        {
            Debug.LogWarning("[DestroyCardEffect] No targets to destroy.");
            return false;
        }

        var destroyedCards = new List<CardInstance>();

        foreach (var target in context.CurrentTargets)
        {
            if (target == null || !target.IsValidTarget)
            {
                Debug.Log("[DestroyCardEffect] Skipping invalid target.");
                continue;
            }

            if (target is CardInstance card)
            {
                bool destroyed = DestroyCard(card, context);
                if (destroyed)
                {
                    destroyedCards.Add(card);
                    Debug.Log($"[DestroyCardEffect] Destroyed: {card.DisplayName}");
                }
            }
            else
            {
                Debug.LogWarning($"[DestroyCardEffect] Cannot destroy non-card target: {target.GetType().Name}");
            }

            // TODO: Wait for destroy animation if needed
            await UniTask.Delay(100);
        }

        // Store destroyed cards for follow-up effects
        if (destroyedCards.Count > 0)
        {
            StoreResult(context, destroyedCards);
        }

        return destroyedCards.Count > 0;
    }

    /// <summary>
    /// Destroy a single card.
    /// </summary>
    private bool DestroyCard(CardInstance card, EffectContext context)
    {
        if (card == null) return false;

        // Set health to 0 (or below) to mark as destroyed
        card.CurrentHealth = 0;

        // Remove from slot
        var slot = card.CurrentSlot;
        if (slot != null)
        {
            slot.RemoveCard();
        }

        // TODO: Move to graveyard zone if you have one
        // TODO: Fire destruction event for triggers
        // EventBus.Publish(new CardDestroyedEvent(card, DestructionCause.Effect));

        return true;
    }

    public override bool CanExecute(EffectContext context)
    {
        if (!base.CanExecute(context))
            return false;

        // Need at least one valid card target
        foreach (var target in context.CurrentTargets)
        {
            if (target is CardInstance && target.IsValidTarget)
                return true;
        }

        return false;
    }

    public override string GetDescription()
    {
        return "Destroy";
    }
}