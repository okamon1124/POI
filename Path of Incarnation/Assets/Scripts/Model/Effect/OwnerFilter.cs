using UnityEngine;

/// <summary>
/// Filter targets by ownership relative to the effect caster.
/// </summary>
[CreateAssetMenu(fileName = "OwnerFilter", menuName = "Effects/Filters/Owner Filter")]
public class OwnerFilter : TargetFilter
{
    public enum OwnershipType
    {
        /// <summary>Targets belonging to the effect's owner.</summary>
        Mine,
        /// <summary>Targets belonging to the opponent.</summary>
        Enemy,
        /// <summary>Any target regardless of owner.</summary>
        Any
    }

    [SerializeField] private OwnershipType ownership = OwnershipType.Any;

    public override bool IsValid(ITargetable target, EffectContext context)
    {
        if (target == null) return false;

        return ownership switch
        {
            OwnershipType.Mine => target.Owner == context.Owner,
            OwnershipType.Enemy => target.Owner != context.Owner && target.Owner != Owner.None,
            OwnershipType.Any => true,
            _ => true
        };
    }

    public override string GetDescription()
    {
        return ownership switch
        {
            OwnershipType.Mine => "your",
            OwnershipType.Enemy => "enemy",
            OwnershipType.Any => "",
            _ => ""
        };
    }
}