public static class CombatSystem
{
    public static CombatResult Resolve(
        PlayerState player,
        PlayerState enemy,
        CombatLane lane)
    {
        var ctx = new CombatContext(player, enemy, lane);

        // 玩家這側：整條 path 上所有卡都有機會套用規則
        foreach (var slot in lane.PlayerPath)
        {
            var card = slot.InSlotCardInstance;
            ApplyRules(card, ctx, isPlayerSide: true);
        }

        // 敵人這側
        foreach (var slot in lane.EnemyPath)
        {
            var card = slot.InSlotCardInstance;
            ApplyRules(card, ctx, isPlayerSide: false);
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