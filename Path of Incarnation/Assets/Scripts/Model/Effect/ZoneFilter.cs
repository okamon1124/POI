using Coffee.UIEffects;
using UnityEngine;

/// <summary>
/// Filter targets by their current zone.
/// </summary>
[CreateAssetMenu(fileName = "ZoneFilter", menuName = "Effects/Filters/Zone Filter")]
public class ZoneFilter : TargetFilter
{
    [SerializeField] private ZoneType[] allowedZones = { ZoneType.Combat, ZoneType.Advance, ZoneType.Deployment };

    public override bool IsValid(ITargetable target, EffectContext context)
    {
        if (target == null) return false;

        // Handle CardInstance targets
        if (target is CardInstance card)
        {
            if (card.CurrentSlot?.Zone == null) return false;

            var zoneType = card.CurrentSlot.Zone.Type;

            foreach (var allowed in allowedZones)
            {
                if (zoneType == allowed)
                    return true;
            }
            return false;
        }

        // Handle Slot targets
        if (target is Slot slot)
        {
            if (slot.Zone == null) return false;

            foreach (var allowed in allowedZones)
            {
                if (slot.Zone.Type == allowed)
                    return true;
            }
            return false;
        }

        // Players don't have zones, so pass if we're filtering players
        if (target.TargetType == TargetType.Player)
            return true;

        return false;
    }

    public override string GetDescription()
    {
        if (allowedZones == null || allowedZones.Length == 0)
            return "";

        if (allowedZones.Length == 1)
            return $"in {allowedZones[0]}";

        return $"on board";
    }
}