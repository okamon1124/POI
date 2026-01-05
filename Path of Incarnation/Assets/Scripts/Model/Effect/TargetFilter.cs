using Coffee.UIEffects;
using UnityEngine;

/// <summary>
/// Base class for target filters.
/// Filters determine which targets are valid for an effect step.
/// Multiple filters combine with AND logic.
/// </summary>
public abstract class TargetFilter : ScriptableObject
{
    /// <summary>
    /// Check if a target passes this filter.
    /// </summary>
    /// <param name="target">The target to check.</param>
    /// <param name="context">The effect context for additional state checks.</param>
    /// <returns>True if target is valid according to this filter.</returns>
    public abstract bool IsValid(ITargetable target, EffectContext context);

    /// <summary>
    /// Get a human-readable description of this filter.
    /// Used for UI and logging.
    /// </summary>
    public abstract string GetDescription();
}