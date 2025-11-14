public class CardInstance
{
    public readonly CardData Data;

    public int CurrentHealth;
    public int CurrentPower;
    public LogicZone CurrentZone;
    public int UniqueId;

    public CardInstance(CardData data, int uniqueId)
    {
        Data = data;
        UniqueId = uniqueId;

        CurrentHealth = data.health;
        CurrentPower = data.power;
        CurrentZone = LogicZone.Deck;
    }
}