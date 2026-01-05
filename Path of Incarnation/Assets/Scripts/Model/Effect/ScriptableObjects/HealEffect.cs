using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SubEffect that heals/restores health to targets.
/// 
/// Example usage:
/// - "Heal 3 to target creature"
/// - "Restore 5 health to your hero"
/// 
/// Writes total amount healed to whiteboard if writeToKey is set.
/// </summary>
[CreateAssetMenu(fileName = "HealEffect", menuName = "Effects/SubEffects/Heal")]
public class HealEffect : SubEffect
{
    [Header("Settings")]
    [SerializeField] private FixedValue amount = new(1);

    public override async UniTask<bool> Execute(EffectContext context)
    {
        int healAmount = amount.Evaluate(context);

        if (healAmount <= 0)
        {
            Debug.LogWarning("[HealEffect] Heal amount is 0 or negative, skipping.");
            return true;
        }

        if (context.CurrentTargets.Count == 0)
        {
            Debug.LogWarning("[HealEffect] No targets to heal.");
            return false;
        }

        int totalHealed = 0;

        foreach (var target in context.CurrentTargets)
        {
            if (target == null || !target.IsValidTarget)
            {
                Debug.Log("[HealEffect] Skipping invalid target.");
                continue;
            }

            int healed = HealTarget(target, healAmount, context);
            totalHealed += healed;

            if (healed > 0)
            {
                Debug.Log($"[HealEffect] Healed {healed} to {target.DisplayName}");
            }

            // TODO: Wait for heal animation if needed
            await UniTask.Delay(100);
        }

        // Store total healed for follow-up effects
        if (totalHealed > 0)
        {
            StoreResult(context, totalHealed);
        }

        return totalHealed > 0;
    }

    private int HealTarget(ITargetable target, int healAmount, EffectContext context)
    {
        switch (target)
        {
            case CardInstance card:
                return HealCard(card, healAmount);

            case PlayerState player:
                return HealPlayer(player, healAmount);

            default:
                Debug.LogWarning($"[HealEffect] Cannot heal target type: {target.GetType().Name}");
                return 0;
        }
    }

    private int HealCard(CardInstance card, int healAmount)
    {
        if (card == null) return 0;

        int previousHealth = card.CurrentHealth;
        card.CurrentHealth += healAmount;
        int actualHeal = card.CurrentHealth - previousHealth;

        // TODO: Fire heal event for UI/triggers
        // EventBus.Publish(new CardHealedEvent(card, actualHeal));

        return actualHeal;
    }

    private int HealPlayer(PlayerState player, int healAmount)
    {
        if (player == null) return 0;

        int previousHealth = player.Health;
        player.Health += healAmount;
        int actualHeal = player.Health - previousHealth;

        // TODO: Fire heal event for UI/triggers
        // EventBus.Publish(new PlayerHealedEvent(player, actualHeal));

        return actualHeal;
    }

    public override bool CanExecute(EffectContext context)
    {
        if (!base.CanExecute(context))
            return false;

        // Need at least one valid target that can be healed
        foreach (var target in context.CurrentTargets)
        {
            if (target != null && target.IsValidTarget)
            {
                // Could add check: is target damaged?
                return true;
            }
        }

        return false;
    }

    public override string GetDescription()
    {
        int value = amount.RawValue;
        return $"Heal {value}";
    }
}