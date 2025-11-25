//public static class CombatResolver
//{
//    /// <summary>
//    /// 解決某一條戰鬥線上的戰鬥。
//    /// playerCombatSlot：玩家這一側的戰鬥格
//    /// enemyCombatSlot：敵人這一側的戰鬥格
//    /// </summary>
//    public static void ResolveCombat(
//        Slot playerCombatSlot,
//        Slot enemyCombatSlot,
//        PlayerState player,
//        PlayerState enemy)
//    {
//        var playerCard = playerCombatSlot?.InSlotCardInstance;
//        var enemyCard = enemyCombatSlot?.InSlotCardInstance;
//
//        // 1) 雙方都沒有卡 → 沒事發生
//        if (playerCard == null && enemyCard == null)
//            return;
//
//        // 2) 雙方都有卡 → 互毆
//        if (playerCard != null && enemyCard != null)
//        {
//            int dmgToPlayerCard = enemyCard.CurrentPower;
//            int dmgToEnemyCard = playerCard.CurrentPower;
//
//            playerCard.CurrentHealth -= dmgToPlayerCard;
//            enemyCard.CurrentHealth -= dmgToEnemyCard;
//            return;
//        }
//
//        // 3) 只有玩家有卡 → 直擊敵方玩家
//        if (playerCard != null && enemyCard == null)
//        {
//            enemy.Health -= playerCard.CurrentPower;
//            return;
//        }
//
//        // 4) 只有敵方有卡 → 直擊玩家
//        if (enemyCard != null && playerCard == null)
//        {
//            player.Health -= enemyCard.CurrentPower;
//            return;
//        }
//    }
//}