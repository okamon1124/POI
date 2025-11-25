using System.Collections.Generic;

public sealed class CombatResult
{
    public int DamageToPlayer { get; }
    public int DamageToEnemy { get; }

    // 每張卡總共被扣多少（邏輯用，算死活、觸發死亡效果等等）
    public IReadOnlyDictionary<CardInstance, int> CardDamages { get; }

    // 視覺層腳本：一格一格播
    public IReadOnlyList<HitEvent> HitEvents { get; }

    public CombatResult(
        int damageToPlayer,
        int damageToEnemy,
        IDictionary<CardInstance, int> cardDamages,
        IList<HitEvent> hitEvents)
    {
        DamageToPlayer = damageToPlayer;
        DamageToEnemy = damageToEnemy;
        CardDamages = new Dictionary<CardInstance, int>(cardDamages);
        HitEvents = new List<HitEvent>(hitEvents);
    }

    public void Apply(PlayerState player, PlayerState enemy)
    {
        player.Health -= DamageToPlayer;
        enemy.Health -= DamageToEnemy;

        foreach (var kv in CardDamages)
        {
            var card = kv.Key;
            var dmg = kv.Value;
            if (card == null || dmg <= 0) continue;

            card.CurrentHealth -= dmg;
        }
    }
}
