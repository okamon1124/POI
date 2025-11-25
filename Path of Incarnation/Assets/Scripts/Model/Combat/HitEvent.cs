public enum HitTargetType
{
    Player,
    Enemy,
    PlayerCard,
    EnemyCard,
}

public struct HitEvent
{
    public CardInstance Source;      // 誰打的（可以是 null 表示系統或陷阱之類）
    public HitTargetType TargetType; // 打向哪一邊
    public CardInstance TargetCard;  // 如果是打卡，就在這裡；打玩家時為 null
    public int Amount;               // 這次扣多少

    public HitEvent(CardInstance source, HitTargetType targetType, CardInstance targetCard, int amount)
    {
        Source = source;
        TargetType = targetType;
        TargetCard = targetCard;
        Amount = amount;
    }
}
