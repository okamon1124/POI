public static class CombatTargeting
{
    /// <summary>
    /// 從敵方 lane 上找距離內的「最近」目標（距離小者優先）。
    /// </summary>
    public static CardInstance FindEnemyInRange(
        CombatContext ctx,
        bool isPlayerSide,
        int range)
    {
        var enemyPath = isPlayerSide ? ctx.Lane.EnemyPath : ctx.Lane.PlayerPath;

        CardInstance bestTarget = null;
        int bestDist = int.MaxValue;

        foreach (var slot in enemyPath)
        {
            var target = slot.InSlotCardInstance;
            if (target == null) continue;

            if (!ctx.Lane.TryGetDistanceFromCombat(slot, out int dist))
                continue;

            if (dist <= range && dist < bestDist)
            {
                bestDist = dist;
                bestTarget = target;
            }
        }

        return bestTarget;
    }
}