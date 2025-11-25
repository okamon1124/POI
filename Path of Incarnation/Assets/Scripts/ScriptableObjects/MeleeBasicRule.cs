using UnityEngine;

[CreateAssetMenu(menuName = "Card Game/Combat/Melee Basic")]
public class MeleeBasicRule : CombatRule
{
    public override void Apply(CombatContext ctx, CardInstance self, bool isPlayerSide)
    {
        var myCombatSlot = isPlayerSide
            ? ctx.Lane.PlayerCombatSlot
            : ctx.Lane.EnemyCombatSlot;

        if (self.CurrentSlot != myCombatSlot)
            return;

        // 近戰：range = 0，打戰鬥格對面的那個
        var target = CombatTargeting.FindEnemyInRange(ctx, isPlayerSide, range: 0);

        if (target != null)
        {
            ctx.DealDamageToCard(self, target, self.CurrentPower);
        }
        else
        {
            // 射程內沒有生物 → 直接打玩家
            if (isPlayerSide)
                ctx.DealDamageToEnemy(self, self.CurrentPower);
            else
                ctx.DealDamageToPlayer(self, self.CurrentPower);
        }
    }
}
