using Coffee.UIEffects;
using UnityEngine;

/// <summary>
/// Filter slot targets by their occupancy state.
/// </summary>
[CreateAssetMenu(fileName = "SlotStateFilter", menuName = "Effects/Filters/Slot State Filter")]
public class SlotStateFilter : TargetFilter
{
    public enum SlotState
    {
        /// <summary>Slot must be empty (no card in it).</summary>
        Empty,
        /// <summary>Slot must be occupied (has a card).</summary>
        Occupied,
        /// <summary>Slot can be either empty or occupied.</summary>
        Any
    }

    [SerializeField] private SlotState requiredState = SlotState.Any;

    public override bool IsValid(ITargetable target, EffectContext context)
    {
        if (target == null) return false;

        // Only applies to slot targets
        if (target is not Slot slot)
            return true; // Non-slots pass through

        return requiredState switch
        {
            SlotState.Empty => slot.IsEmpty,
            SlotState.Occupied => !slot.IsEmpty,
            SlotState.Any => true,
            _ => true
        };
    }

    public override string GetDescription()
    {
        return requiredState switch
        {
            SlotState.Empty => "empty",
            SlotState.Occupied => "occupied",
            SlotState.Any => "",
            _ => ""
        };
    }
}