using System;
using System.Collections.Generic;

/// <summary>
/// 一條戰鬥線（Lane），包含玩家 / 敵人兩側的 Slot 路徑，
/// 並能計算距離戰鬥格的距離。
/// </summary>
public sealed class CombatLane
{
    public IReadOnlyList<Slot> PlayerPath { get; }
    public IReadOnlyList<Slot> EnemyPath { get; }

    public Slot PlayerCombatSlot => PlayerPath[^1];
    public Slot EnemyCombatSlot => EnemyPath[^1];

    private readonly Dictionary<Slot, int> _distanceFromCombat = new();

    public CombatLane(List<Slot> playerPath, List<Slot> enemyPath)
    {
        PlayerPath = playerPath;
        EnemyPath = enemyPath;

        BuildDistanceMap(playerPath);
        BuildDistanceMap(enemyPath);
    }

    private void BuildDistanceMap(List<Slot> path)
    {
        int combatIndex = path.Count - 1;  // 最後一格是戰鬥格

        for (int i = 0; i < path.Count; i++)
        {
            Slot slot = path[i];
            int dist = combatIndex - i;    // 戰鬥格 = 0, 再外面 = 1, 2, ...
            _distanceFromCombat[slot] = dist;
        }
    }

    public bool TryGetDistanceFromCombat(Slot slot, out int dist)
        => _distanceFromCombat.TryGetValue(slot, out dist);

    public bool Contains(Slot slot)
        => _distanceFromCombat.ContainsKey(slot);
}
