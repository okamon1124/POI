using UnityEngine;

[CreateAssetMenu(menuName = "Card Game/Combat/Ranged Single")]
public class RangedRule : CombatRule
{
    [SerializeField] private int range = 2;

    public override void Apply(CombatContext ctx, CardInstance self, bool isPlayerSide)
    {
        var myCombatSlot = isPlayerSide
            ? ctx.Lane.PlayerCombatSlot
            : ctx.Lane.EnemyCombatSlot;

        if (self.CurrentSlot != myCombatSlot)
            return;

        var target = CombatTargeting.FindEnemyInRange(ctx, isPlayerSide, range);

        if (target != null)
        {
            ctx.DealDamageToCard(self, target, self.CurrentPower);
        }
        else
        {
            if (isPlayerSide)
                ctx.DealDamageToEnemy(self, self.CurrentPower);
            else
                ctx.DealDamageToPlayer(self, self.CurrentPower);
        }
    }
}
