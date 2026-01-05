using Coffee.UIEffects;
using UnityEngine;

/// <summary>
/// Filter card targets by their card type.
/// </summary>
[CreateAssetMenu(fileName = "CardTypeFilter", menuName = "Effects/Filters/Card Type Filter")]
public class CardTypeFilter : TargetFilter
{
    [SerializeField] private CardType[] allowedTypes = { CardType.Creature };

    public override bool IsValid(ITargetable target, EffectContext context)
    {
        if (target == null) return false;

        // Only applies to card targets
        if (target is not CardInstance card)
            return true; // Non-cards pass through (filter doesn't apply)

        if (card.Data == null) return false;

        foreach (var allowed in allowedTypes)
        {
            if (card.Data.cardType == allowed)
                return true;
        }

        return false;
    }

    public override string GetDescription()
    {
        if (allowedTypes == null || allowedTypes.Length == 0)
            return "card";

        if (allowedTypes.Length == 1)
            return allowedTypes[0].ToString().ToLower();

        return "card";
    }
}