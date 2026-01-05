using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A complete effect that can be attached to a card.
/// Contains a list of EffectSteps that are executed in order.
/// 
/// Example effects:
/// - "Draw 2 cards" ¡÷ 1 step
/// - "Deal 3 damage to target creature" ¡÷ 1 step with targeting
/// - "Draw 2 cards, then discard 1 of them" ¡÷ 2 steps with whiteboard passing
/// - "Deal 2 damage to target creature. If it dies, draw a card." ¡÷ 2 steps with conditional
/// </summary>
[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Effect")]
public class Effect : ScriptableObject
{
    [Header("Description")]
    [Tooltip("Display name for this effect (shown in UI).")]
    [SerializeField] private string effectName;

    [Tooltip("Detailed description of what this effect does.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Steps")]
    [Tooltip("The steps to execute in order.")]
    [SerializeField] private List<EffectStep> steps = new();

    [Header("Targeting Requirements")]
    [Tooltip("If true, all targeting must be valid before any step executes. " +
             "If false, targeting happens step-by-step.")]
    [SerializeField] private bool requireAllTargetsUpfront = false;

    // ========================= Properties =========================

    public string EffectName => effectName;
    public string Description => description;
    public IReadOnlyList<EffectStep> Steps => steps;
    public bool RequireAllTargetsUpfront => requireAllTargetsUpfront;
    public int StepCount => steps.Count;

    // ========================= Validation =========================

    /// <summary>
    /// Check if this effect has any steps.
    /// </summary>
    public bool HasSteps => steps != null && steps.Count > 0;

    /// <summary>
    /// Check if this effect requires any manual targeting.
    /// </summary>
    public bool RequiresManualTargeting
    {
        get
        {
            foreach (var step in steps)
            {
                if (step.RequiresManualTargeting)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Get all steps that require manual targeting.
    /// </summary>
    public List<EffectStep> GetManualTargetingSteps()
    {
        var result = new List<EffectStep>();
        foreach (var step in steps)
        {
            if (step.RequiresManualTargeting)
                result.Add(step);
        }
        return result;
    }

    /// <summary>
    /// Get step at index, or null if out of range.
    /// </summary>
    public EffectStep GetStep(int index)
    {
        if (index < 0 || index >= steps.Count)
            return null;
        return steps[index];
    }

    // ========================= Description Generation =========================

    /// <summary>
    /// Generate a complete description from all steps.
    /// Useful for auto-generating card text.
    /// </summary>
    public string GenerateDescription()
    {
        if (steps == null || steps.Count == 0)
            return "No effect.";

        var descriptions = new List<string>();
        foreach (var step in steps)
        {
            string desc = step.GetDescription();
            if (!string.IsNullOrEmpty(desc))
                descriptions.Add(desc);
        }

        return string.Join(". ", descriptions) + ".";
    }

    // ========================= Editor Helpers =========================

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-generate description if empty
        if (string.IsNullOrEmpty(description) && steps != null && steps.Count > 0)
        {
            description = GenerateDescription();
        }
    }
#endif
}