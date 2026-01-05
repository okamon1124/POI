using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single step in an effect's resolution.
/// Combines targeting configuration with a SubEffect action.
/// 
/// Example steps:
/// - "Draw 2 cards": targetType=None, subEffect=DrawCardsEffect
/// - "Deal 3 damage to target creature": targetType=Creature, mode=Manual, subEffect=DealDamageEffect
/// - "Deal 1 damage to all enemies": targetType=Creature, mode=All, filters=[EnemyFilter], subEffect=DealDamageEffect
/// </summary>
[Serializable]
public class EffectStep
{
    [Header("Targeting")]
    [Tooltip("What type of thing can be targeted.")]
    [SerializeField] private TargetType targetType = TargetType.None;

    [Tooltip("How targets are selected.")]
    [SerializeField] private TargetingMode targetingMode = TargetingMode.None;

    [Tooltip("Number of targets to select. Use -1 for 'all valid targets'.")]
    [SerializeField] private int targetCount = 1;

    [Tooltip("Filters to determine which targets are valid.")]
    [SerializeField] private List<TargetFilter> filters = new();

    [Header("Whiteboard")]
    [Tooltip("If set, read targets from this whiteboard key instead of selecting new targets.")]
    [SerializeField] private string readFromKey;

    [Tooltip("If set, write selected targets to this whiteboard key for later steps.")]
    [SerializeField] private string writeTargetsToKey;

    [Header("Action")]
    [Tooltip("The SubEffect to execute on the targets.")]
    [SerializeField] private SubEffect subEffect;

    [Header("Options")]
    [Tooltip("If true, the effect continues even if this step fails.")]
    [SerializeField] private bool optional = false;

    // ========================= Properties =========================

    public TargetType TargetType => targetType;
    public TargetingMode TargetingMode => targetingMode;
    public int TargetCount => targetCount;
    public IReadOnlyList<TargetFilter> Filters => filters;
    public string ReadFromKey => readFromKey;
    public string WriteTargetsToKey => writeTargetsToKey;
    public SubEffect SubEffect => subEffect;
    public bool Optional => optional;

    // ========================= Validation =========================

    /// <summary>
    /// Check if a potential target passes all filters.
    /// </summary>
    public bool PassesFilters(ITargetable target, EffectContext context)
    {
        if (target == null) return false;

        // Check target type matches
        if (targetType != TargetType.None)
        {
            // Special case: Creature is a subset of Card
            if (targetType == TargetType.Creature)
            {
                if (target.TargetType != TargetType.Creature && target.TargetType != TargetType.Card)
                    return false;

                // If it's a Card type, verify it's actually a creature
                if (target is CardInstance card && card.Data?.cardType != CardType.Creature)
                    return false;
            }
            else if (target.TargetType != targetType)
            {
                return false;
            }
        }

        // Check all filters
        foreach (var filter in filters)
        {
            if (filter == null) continue;

            if (!filter.IsValid(target, context))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if this step requires manual target selection.
    /// </summary>
    public bool RequiresManualTargeting => targetingMode == TargetingMode.Manual;

    /// <summary>
    /// Check if this step reads targets from the whiteboard.
    /// </summary>
    public bool ReadsFromWhiteboard =>
        targetingMode == TargetingMode.FromWhiteboard || !string.IsNullOrEmpty(readFromKey);

    /// <summary>
    /// Check if this step has a valid SubEffect assigned.
    /// </summary>
    public bool HasSubEffect => subEffect != null;

    // ========================= Description =========================

    /// <summary>
    /// Generate a human-readable description of this step.
    /// </summary>
    public string GetDescription()
    {
        if (subEffect == null)
            return "[No SubEffect]";

        string effectDesc = subEffect.GetDescription();

        // Build target description
        string targetDesc = "";
        if (targetType != TargetType.None && targetingMode != TargetingMode.None)
        {
            // Gather filter descriptions
            var filterDescs = new List<string>();
            foreach (var filter in filters)
            {
                if (filter == null) continue;
                string desc = filter.GetDescription();
                if (!string.IsNullOrEmpty(desc))
                    filterDescs.Add(desc);
            }

            string filterStr = filterDescs.Count > 0 ? string.Join(" ", filterDescs) + " " : "";

            targetDesc = targetingMode switch
            {
                TargetingMode.Manual => $" to target {filterStr}{targetType.ToString().ToLower()}",
                TargetingMode.All => $" to all {filterStr}{targetType.ToString().ToLower()}s",
                TargetingMode.Random => $" to random {filterStr}{targetType.ToString().ToLower()}",
                TargetingMode.Self => " to self",
                _ => ""
            };
        }

        return effectDesc + targetDesc;
    }
}