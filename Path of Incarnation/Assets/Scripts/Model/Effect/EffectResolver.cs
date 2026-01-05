using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executes Effects step-by-step.
/// Handles target gathering, manual targeting (via callback), and SubEffect execution.
/// 
/// Usage:
///     var resolver = new EffectResolver();
///     var context = new EffectContext(...);
///     bool success = await resolver.Execute(effect, context);
/// </summary>
public class EffectResolver
{
    // ========================= Delegates =========================

    /// <summary>
    /// Callback for manual target selection.
    /// UI should show valid targets and wait for player selection.
    /// Returns the selected targets, or empty list if cancelled.
    /// </summary>
    public delegate UniTask<List<ITargetable>> ManualTargetSelectionHandler(
        EffectStep step,
        List<ITargetable> validTargets,
        int requiredCount,
        EffectContext context
    );

    /// <summary>
    /// Set this to handle manual target selection.
    /// If not set, manual targeting steps will fail.
    /// </summary>
    public ManualTargetSelectionHandler OnManualTargetSelection { get; set; }

    // ========================= Main Execution =========================

    /// <summary>
    /// Execute an effect completely.
    /// </summary>
    /// <param name="effect">The effect to execute.</param>
    /// <param name="context">The effect context (whiteboard, references, etc.).</param>
    /// <returns>True if the effect completed successfully.</returns>
    public async UniTask<bool> Execute(Effect effect, EffectContext context)
    {
        if (effect == null)
        {
            Debug.LogError("[EffectResolver] Cannot execute null effect.");
            return false;
        }

        if (!effect.HasSteps)
        {
            Debug.LogWarning($"[EffectResolver] Effect '{effect.EffectName}' has no steps.");
            return true; // Empty effect is not a failure
        }

        Debug.Log($"[EffectResolver] Executing effect: {effect.EffectName}");

        // Execute each step in order
        for (int i = 0; i < effect.StepCount; i++)
        {
            var step = effect.GetStep(i);
            context.CurrentStepIndex = i;

            Debug.Log($"[EffectResolver] Step {i + 1}/{effect.StepCount}: {step.GetDescription()}");

            bool stepSuccess = await ExecuteStep(step, context);

            if (!stepSuccess && !step.Optional)
            {
                Debug.LogWarning($"[EffectResolver] Step {i + 1} failed and is not optional. Stopping effect.");
                return false;
            }

            // Check if effect was cancelled
            if (context.IsCancelled)
            {
                Debug.Log($"[EffectResolver] Effect cancelled: {context.CancellationReason}");
                return false;
            }
        }

        Debug.Log($"[EffectResolver] Effect '{effect.EffectName}' completed successfully.");
        return true;
    }

    // ========================= Step Execution =========================

    private async UniTask<bool> ExecuteStep(EffectStep step, EffectContext context)
    {
        // 1. Clear previous targets
        context.ClearCurrentTargets();

        // 2. Gather/select targets
        bool targetingSuccess = await GatherTargets(step, context);
        if (!targetingSuccess)
        {
            Debug.LogWarning("[EffectResolver] Targeting failed.");
            return false;
        }

        // 3. Store targets to whiteboard if configured
        if (!string.IsNullOrEmpty(step.WriteTargetsToKey) && context.CurrentTargets.Count > 0)
        {
            context.Store(step.WriteTargetsToKey, new List<ITargetable>(context.CurrentTargets));
        }

        // 4. Execute the SubEffect
        if (!step.HasSubEffect)
        {
            Debug.LogWarning("[EffectResolver] Step has no SubEffect.");
            return step.Optional;
        }

        if (!step.SubEffect.CanExecute(context))
        {
            Debug.LogWarning($"[EffectResolver] SubEffect '{step.SubEffect.name}' cannot execute.");
            return step.Optional;
        }

        bool success = await step.SubEffect.Execute(context);
        return success;
    }

    // ========================= Target Gathering =========================

    private async UniTask<bool> GatherTargets(EffectStep step, EffectContext context)
    {
        // No targeting needed
        if (step.TargetType == TargetType.None || step.TargetingMode == TargetingMode.None)
        {
            return true;
        }

        // Read from whiteboard
        if (step.ReadsFromWhiteboard)
        {
            return GatherTargetsFromWhiteboard(step, context);
        }

        // Gather valid targets from the game state
        var validTargets = GatherValidTargets(step, context);

        // Select targets based on mode
        return step.TargetingMode switch
        {
            TargetingMode.Manual => await GatherManualTargets(step, validTargets, context),
            TargetingMode.All => GatherAllTargets(validTargets, context),
            TargetingMode.Random => GatherRandomTargets(step, validTargets, context),
            TargetingMode.Self => GatherSelfTarget(step, context),
            TargetingMode.Source => GatherSourceTarget(context),
            _ => false
        };
    }

    /// <summary>
    /// Collect all potential targets from the game state that pass filters.
    /// </summary>
    private List<ITargetable> GatherValidTargets(EffectStep step, EffectContext context)
    {
        var validTargets = new List<ITargetable>();

        // Gather based on target type
        switch (step.TargetType)
        {
            case TargetType.Card:
            case TargetType.Creature:
                GatherCardTargets(step, context, validTargets);
                break;

            case TargetType.Slot:
                GatherSlotTargets(step, context, validTargets);
                break;

            case TargetType.Player:
                GatherPlayerTargets(step, context, validTargets);
                break;
        }

        return validTargets;
    }

    private void GatherCardTargets(EffectStep step, EffectContext context, List<ITargetable> results)
    {
        // Iterate through all slots on the board
        foreach (var slot in context.Board.GetAllSlots())
        {
            var card = slot.InSlotCardInstance;
            if (card == null) continue;

            // Check if card implements ITargetable
            if (card is ITargetable targetable && targetable.IsValidTarget)
            {
                if (step.PassesFilters(targetable, context))
                {
                    results.Add(targetable);
                }
            }
        }
    }

    private void GatherSlotTargets(EffectStep step, EffectContext context, List<ITargetable> results)
    {
        foreach (var slot in context.Board.GetAllSlots())
        {
            // Check if slot implements ITargetable
            if (slot is ITargetable targetable && targetable.IsValidTarget)
            {
                if (step.PassesFilters(targetable, context))
                {
                    results.Add(targetable);
                }
            }
        }
    }

    private void GatherPlayerTargets(EffectStep step, EffectContext context, List<ITargetable> results)
    {
        // Check owner
        if (context.OwnerState is ITargetable ownerTarget && ownerTarget.IsValidTarget)
        {
            if (step.PassesFilters(ownerTarget, context))
            {
                results.Add(ownerTarget);
            }
        }

        // Check opponent
        if (context.OpponentState is ITargetable opponentTarget && opponentTarget.IsValidTarget)
        {
            if (step.PassesFilters(opponentTarget, context))
            {
                results.Add(opponentTarget);
            }
        }
    }

    // ========================= Target Selection Modes =========================

    private async UniTask<bool> GatherManualTargets(
        EffectStep step,
        List<ITargetable> validTargets,
        EffectContext context)
    {
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("[EffectResolver] No valid targets for manual selection.");
            return false;
        }

        if (OnManualTargetSelection == null)
        {
            Debug.LogError("[EffectResolver] Manual targeting required but no handler set.");
            return false;
        }

        int requiredCount = step.TargetCount == -1 ? validTargets.Count : step.TargetCount;

        // Call the UI handler to let player select
        var selected = await OnManualTargetSelection(step, validTargets, requiredCount, context);

        if (selected == null || selected.Count == 0)
        {
            Debug.Log("[EffectResolver] Manual targeting cancelled or no targets selected.");
            return false;
        }

        // Validate selected targets
        foreach (var target in selected)
        {
            if (target != null && step.PassesFilters(target, context))
            {
                context.AddTarget(target);
            }
        }

        // Check if we got enough targets
        if (step.TargetCount != -1 && context.CurrentTargets.Count < step.TargetCount)
        {
            Debug.LogWarning($"[EffectResolver] Not enough targets selected. " +
                           $"Required: {step.TargetCount}, Got: {context.CurrentTargets.Count}");
            return false;
        }

        return context.CurrentTargets.Count > 0;
    }

    private bool GatherAllTargets(List<ITargetable> validTargets, EffectContext context)
    {
        foreach (var target in validTargets)
        {
            context.AddTarget(target);
        }

        return true; // "All" can be zero targets and still succeed
    }

    private bool GatherRandomTargets(EffectStep step, List<ITargetable> validTargets, EffectContext context)
    {
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("[EffectResolver] No valid targets for random selection.");
            return false;
        }

        int count = step.TargetCount == -1 ? validTargets.Count : Mathf.Min(step.TargetCount, validTargets.Count);

        // Shuffle and take
        var shuffled = new List<ITargetable>(validTargets);
        ShuffleList(shuffled);

        for (int i = 0; i < count; i++)
        {
            context.AddTarget(shuffled[i]);
        }

        return context.CurrentTargets.Count > 0;
    }

    private bool GatherSelfTarget(EffectStep step, EffectContext context)
    {
        // "Self" typically means the caster/owner
        if (step.TargetType == TargetType.Player)
        {
            if (context.OwnerState is ITargetable ownerTarget)
            {
                context.AddTarget(ownerTarget);
                return true;
            }
        }
        else if (step.TargetType == TargetType.Card || step.TargetType == TargetType.Creature)
        {
            // Self for a card effect usually means the source card
            if (context.Source is ITargetable sourceTarget)
            {
                context.AddTarget(sourceTarget);
                return true;
            }
        }

        Debug.LogWarning("[EffectResolver] Could not determine self target.");
        return false;
    }

    private bool GatherSourceTarget(EffectContext context)
    {
        if (context.Source is ITargetable sourceTarget && sourceTarget.IsValidTarget)
        {
            context.AddTarget(sourceTarget);
            return true;
        }

        Debug.LogWarning("[EffectResolver] Source is not a valid target.");
        return false;
    }

    private bool GatherTargetsFromWhiteboard(EffectStep step, EffectContext context)
    {
        string key = step.ReadFromKey;
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[EffectResolver] ReadFromKey is empty.");
            return false;
        }

        // Try to get list of targetables
        if (context.TryRetrieve<List<ITargetable>>(key, out var targets))
        {
            foreach (var target in targets)
            {
                if (target != null && target.IsValidTarget && step.PassesFilters(target, context))
                {
                    context.AddTarget(target);
                }
            }
            return true;
        }

        // Try to get list of cards (common case)
        if (context.TryRetrieve<List<CardInstance>>(key, out var cards))
        {
            foreach (var card in cards)
            {
                if (card is ITargetable targetable && targetable.IsValidTarget && step.PassesFilters(targetable, context))
                {
                    context.AddTarget(targetable);
                }
            }
            return true;
        }

        Debug.LogWarning($"[EffectResolver] Could not find targets in whiteboard key '{key}'.");
        return false;
    }

    // ========================= Utility =========================

    private void ShuffleList<T>(List<T> list)
    {
        var rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}