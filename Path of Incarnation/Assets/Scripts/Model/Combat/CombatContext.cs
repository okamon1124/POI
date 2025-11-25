using System.Collections.Generic;

public class CombatContext
{
    public PlayerState Player;
    public PlayerState Enemy;
    public CombatLane Lane;

    // 總結：算死活時好用
    public int DamageToPlayer { get; private set; }
    public int DamageToEnemy { get; private set; }

    // 統計每張卡總共被打多少（給邏輯 / 推算死亡）
    private readonly Dictionary<CardInstance, int> _cardDamages = new();

    // 給視覺層使用的完整事件列表
    private readonly List<HitEvent> _hitEvents = new();

    public CombatContext(PlayerState player, PlayerState enemy, CombatLane lane)
    {
        Player = player;
        Enemy = enemy;
        Lane = lane;
    }

    // ----------- 封裝好的傷害 API -----------

    public void DealDamageToPlayer(CardInstance source, int amount)
    {
        if (amount <= 0) return;
        DamageToPlayer += amount;
        _hitEvents.Add(new HitEvent(source, HitTargetType.Player, null, amount));
    }

    public void DealDamageToEnemy(CardInstance source, int amount)
    {
        if (amount <= 0) return;
        DamageToEnemy += amount;
        _hitEvents.Add(new HitEvent(source, HitTargetType.Enemy, null, amount));
    }

    public void DealDamageToPlayerCard(CardInstance source, CardInstance target, int amount)
    {
        if (amount <= 0 || target == null) return;

        if (_cardDamages.TryGetValue(target, out var cur))
            _cardDamages[target] = cur + amount;
        else
            _cardDamages[target] = amount;

        _hitEvents.Add(new HitEvent(source, HitTargetType.PlayerCard, target, amount));
    }

    public void DealDamageToEnemyCard(CardInstance source, CardInstance target, int amount)
    {
        if (amount <= 0 || target == null) return;

        if (_cardDamages.TryGetValue(target, out var cur))
            _cardDamages[target] = cur + amount;
        else
            _cardDamages[target] = amount;

        _hitEvents.Add(new HitEvent(source, HitTargetType.EnemyCard, target, amount));
    }

    // 對任意 card 的統一入口（如果你不想分 Player/Enemy 也可以只留這個）
    public void DealDamageToCard(CardInstance source, CardInstance target, int amount)
    {
        if (amount <= 0 || target == null) return;

        if (_cardDamages.TryGetValue(target, out var cur))
            _cardDamages[target] = cur + amount;
        else
            _cardDamages[target] = amount;

        // TargetType 可以改成用 Owner 算
        var type = target.Owner == Owner.Player
            ? HitTargetType.PlayerCard
            : HitTargetType.EnemyCard;

        _hitEvents.Add(new HitEvent(source, type, target, amount));
    }

    // ----------- 輸出結果 -----------

    public CombatResult ToResult()
    {
        return new CombatResult(
            DamageToPlayer,
            DamageToEnemy,
            _cardDamages,
            _hitEvents
        );
    }
}
