using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SubEffect that deals damage to the current targets.
/// Targets are determined by the EffectStep's targeting settings.
/// 
/// Example usage:
/// - "Deal 3 damage to target creature" ¡÷ amount = 3, single target
/// - "Deal 1 damage to all enemy creatures" ¡÷ amount = 1, AoE targeting
/// - "Deal 5 damage to the enemy player" ¡÷ amount = 5, target = player
/// 
/// Writes total damage dealt to whiteboard if writeToKey is set,
/// allowing follow-up effects like "Deal 3 damage, gain that much life".
/// </summary>
[CreateAssetMenu(fileName = "DealDamageEffect", menuName = "Effects/SubEffects/Deal Damage")]
public class DealDamageEffect : SubEffect
{
    [Header("Settings")]
    [SerializeField] private FixedValue amount = new(1);

    public override async UniTask<bool> Execute(EffectContext context)
    {
        int damageAmount = amount.Evaluate(context);

        if (damageAmount <= 0)
        {
            Debug.LogWarning("[DealDamageEffect] Damage amount is 0 or negative, skipping.");
            return true;
        }

        if (context.CurrentTargets.Count == 0)
        {
            Debug.LogWarning("[DealDamageEffect] No targets to damage.");
            return false;
        }

        int totalDamageDealt = 0;

        foreach (var target in context.CurrentTargets)
        {
            if (target == null || !target.IsValidTarget)
            {
                Debug.Log($"[DealDamageEffect] Skipping invalid target.");
                continue;
            }

            int damageDealt = DealDamageToTarget(target, damageAmount, context);
            totalDamageDealt += damageDealt;

            Debug.Log($"[DealDamageEffect] Dealt {damageDealt} damage to {target.DisplayName}");

            // TODO: Wait for damage animation if needed
            await UniTask.Delay(100);
        }

        // Store total damage dealt for follow-up effects
        if (totalDamageDealt > 0)
        {
            StoreResult(context, totalDamageDealt);
        }

        return totalDamageDealt > 0;
    }

    /// <summary>
    /// Apply damage to a single target.
    /// Returns actual damage dealt (might differ due to shields, etc.)
    /// </summary>
    private int DealDamageToTarget(ITargetable target, int damage, EffectContext context)
    {
        switch (target)
        {
            case CardInstance card:
                return DealDamageToCard(card, damage);

            case PlayerState player:
                return DealDamageToPlayer(player, damage);

            default:
                Debug.LogWarning($"[DealDamageEffect] Cannot deal damage to target type: {target.GetType().Name}");
                return 0;
        }
    }

    private int DealDamageToCard(CardInstance card, int damage)
    {
        if (card == null) return 0;

        int previousHealth = card.CurrentHealth;
        card.CurrentHealth -= damage;
        int actualDamage = previousHealth - card.CurrentHealth;

        // TODO: Fire damage event for UI/triggers
        // EventBus.Publish(new CardDamagedEvent(card, actualDamage));

        // Check for death
        if (card.CurrentHealth <= 0)
        {
            Debug.Log($"[DealDamageEffect] {card.DisplayName} was destroyed!");
            // TODO: Fire death event
            // EventBus.Publish(new CardDestroyedEvent(card));
        }

        return actualDamage;
    }

    private int DealDamageToPlayer(PlayerState player, int damage)
    {
        if (player == null) return 0;

        int previousHealth = player.Health;
        player.Health -= damage;
        int actualDamage = previousHealth - player.Health;

        // TODO: Fire damage event for UI/triggers
        // EventBus.Publish(new PlayerDamagedEvent(player, actualDamage));

        return actualDamage;
    }

    public override bool CanExecute(EffectContext context)
    {
        if (!base.CanExecute(context))
            return false;

        // Need at least one valid target
        foreach (var target in context.CurrentTargets)
        {
            if (target != null && target.IsValidTarget)
                return true;
        }

        return false;
    }

    public override string GetDescription()
    {
        int value = amount.RawValue;
        return $"Deal {value} damage";
    }
}