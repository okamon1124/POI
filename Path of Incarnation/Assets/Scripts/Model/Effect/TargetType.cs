/// <summary>
/// Defines what category of game object can be targeted by an effect.
/// </summary>
public enum TargetType
{
    /// <summary>
    /// No target needed. Effect applies automatically (e.g., "Draw 2 cards").
    /// </summary>
    None = 0,

    /// <summary>
    /// Target is a card (any type - creature, spell, etc.).
    /// Can be on board, in hand, or other zones depending on filters.
    /// </summary>
    Card = 1,

    /// <summary>
    /// Target is specifically a creature on the board.
    /// </summary>
    Creature = 2,

    /// <summary>
    /// Target is a board slot/cell.
    /// Can be empty or occupied depending on filters.
    /// </summary>
    Slot = 3,

    /// <summary>
    /// Target is a player (for direct damage, mill, life gain, etc.).
    /// </summary>
    Player = 4,
}