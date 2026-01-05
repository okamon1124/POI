public static class CombatSystem
{
    public static CombatResult Resolve(
        PlayerState player,
        PlayerState enemy,
        CombatLane lane,
        bool isPlayerTurn)
    {
        var ctx = new CombatContext(player, enemy, lane);

        if (isPlayerTurn)
        {
            // Player's turn: only player's creature in combat slot attacks
            var attacker = lane.PlayerCombatSlot.InSlotCardInstance;
            ApplyRules(attacker, ctx, isPlayerSide: true);
        }
        else
        {
            // Enemy's turn: only enemy's creature in combat slot attacks
            var attacker = lane.EnemyCombatSlot.InSlotCardInstance;
            ApplyRules(attacker, ctx, isPlayerSide: false);
        }

        return ctx.ToResult();
    }

    private static void ApplyRules(CardInstance card, CombatContext ctx, bool isPlayerSide)
    {
        if (card == null) return;
        if (card.ActiveCombatRules == null) return;

        foreach (var rule in card.ActiveCombatRules)
        {
            rule.Apply(ctx, card, isPlayerSide);
        }
    }
}