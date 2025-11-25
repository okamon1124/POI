using UnityEngine;

public abstract class CombatRule : ScriptableObject
{
    /// <summary>
    /// 套用這個戰鬥規則。
    /// self: 這張卡本身
    /// isPlayerSide: true = 玩家這邊, false = 敵人這邊
    /// </summary>
    public abstract void Apply(CombatContext ctx, CardInstance self, bool isPlayerSide);
}
