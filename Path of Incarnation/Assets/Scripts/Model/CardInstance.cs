using System.Collections.Generic;

public class CardInstance
{
    // Static counter for draw order - each new card gets the next number
    private static int _nextDrawOrder = 0;

    public CardData Data { get; }
    public Owner Owner { get; }
    public Slot CurrentSlot { get; set; }
    public int CurrentPower { get; set; }
    public int CurrentSpeed { get; set; }
    public int CurrentHealth { get; set; }

    /// <summary>
    /// Order in which this card was created/drawn.
    /// Used by HandSplineLayout to maintain consistent left-to-right ordering.
    /// Lower values = created earlier = appears on left.
    /// </summary>
    public int DrawOrder { get; }

    /// <summary>
    /// 這張卡「目前生效」的戰鬥規則。
    /// 一開始會從 Data.combatRules 拷貝進來，之後可以在遊戲中加/減。
    /// </summary>
    public List<CombatRule> ActiveCombatRules { get; } = new List<CombatRule>();

    public CardInstance(CardData data, Owner owner)
    {
        Data = data;
        Owner = owner;
        CurrentPower = data.power;
        CurrentHealth = data.health;
        CurrentSpeed = data.speed;

        // Assign draw order - newer cards get higher numbers
        DrawOrder = _nextDrawOrder++;

        InitCombatRulesFromData();
    }

    private void InitCombatRulesFromData()
    {
        ActiveCombatRules.Clear();
        if (Data != null && Data.combatRules != null)
            ActiveCombatRules.AddRange(Data.combatRules);
    }

    /// <summary>
    /// 在遊戲中暫時加入一個戰鬥規則（例如本回合獲得 Trample）。
    /// </summary>
    public void AddCombatRule(CombatRule rule)
    {
        if (rule == null) return;
        if (!ActiveCombatRules.Contains(rule))
            ActiveCombatRules.Add(rule);
    }

    /// <summary>
    /// 在遊戲中移除一個戰鬥規則（buff 結束等等）。
    /// </summary>
    public void RemoveCombatRule(CombatRule rule)
    {
        if (rule == null) return;
        ActiveCombatRules.Remove(rule);
    }

    /// <summary>
    /// Reset the draw order counter. Call this when starting a new game.
    /// </summary>
    public static void ResetDrawOrderCounter()
    {
        _nextDrawOrder = 0;
    }
}