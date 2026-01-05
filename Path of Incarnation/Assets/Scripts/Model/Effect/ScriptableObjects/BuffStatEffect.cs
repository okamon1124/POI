using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SubEffect that buffs/debuffs a stat on target cards.
/// 
/// Example usage:
/// - "Give target creature +2/+2" ¡÷ Two BuffStatEffects, one for power, one for health
/// - "Target creature gets -1 power" ¡÷ statType = Power, amount = -1
/// - "Your creatures get +1/+0" ¡÷ AoE targeting with power buff
/// </summary>
[CreateAssetMenu(fileName = "BuffStatEffect", menuName = "Effects/SubEffects/Buff Stat")]
public class BuffStatEffect : SubEffect
{
    public enum StatType
    {
        Power,
        Health,
        Speed
    }

    [Header("Settings")]
    [SerializeField] private StatType statType = StatType.Power;
    [SerializeField] private FixedValue amount = new(1);

    public override async UniTask<bool> Execute(EffectContext context)
    {
        int buffAmount = amount.Evaluate(context);

        if (buffAmount == 0)
        {
            Debug.LogWarning("[BuffStatEffect] Buff amount is 0, skipping.");
            return true;
        }

        if (context.CurrentTargets.Count == 0)
        {
            Debug.LogWarning("[BuffStatEffect] No targets to buff.");
            return false;
        }

        int targetsBuffed = 0;

        foreach (var target in context.CurrentTargets)
        {
            if (target == null || !target.IsValidTarget)
            {
                Debug.Log("[BuffStatEffect] Skipping invalid target.");
                continue;
            }

            if (target is CardInstance card)
            {
                ApplyBuff(card, buffAmount);
                targetsBuffed++;

                string change = buffAmount > 0 ? $"+{buffAmount}" : buffAmount.ToString();
                Debug.Log($"[BuffStatEffect] {card.DisplayName} got {change} {statType}");
            }
            else
            {
                Debug.LogWarning($"[BuffStatEffect] Cannot buff non-card target: {target.GetType().Name}");
            }

            // TODO: Wait for buff animation if needed
            await UniTask.Delay(100);
        }

        return targetsBuffed > 0;
    }

    private void ApplyBuff(CardInstance card, int buffAmount)
    {
        switch (statType)
        {
            case StatType.Power:
                card.CurrentPower += buffAmount;
                // Ensure power doesn't go below 0
                if (card.CurrentPower < 0) card.CurrentPower = 0;
                break;

            case StatType.Health:
                card.CurrentHealth += buffAmount;
                // Note: Health CAN go below 0 (creature dies)
                // But for buffs specifically, you might want minimum of 1
                if (buffAmount > 0 && card.CurrentHealth < 1) card.CurrentHealth = 1;
                break;

            case StatType.Speed:
                card.CurrentSpeed += buffAmount;
                if (card.CurrentSpeed < 0) card.CurrentSpeed = 0;
                break;
        }

        // TODO: Fire stat changed event for UI updates
        // EventBus.Publish(new CardStatChangedEvent(card, statType, buffAmount));
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
        int value = amount.RawValue;
        string sign = value >= 0 ? "+" : "";
        return $"{sign}{value} {statType}";
    }
}