/// <summary>
/// Defines all game phases in order.
/// </summary>
public enum PhaseType
{
    /// <summary>
    /// Player draws a card at the beginning of their turn.
    /// </summary>
    Draw,

    /// <summary>
    /// Player can play cards from hand to the board.
    /// Ends when player presses "End Main Phase" button.
    /// </summary>
    Main,

    /// <summary>
    /// All creatures advance one step along their paths automatically.
    /// </summary>
    Movement,

    /// <summary>
    /// Creatures in combat zones fight automatically.
    /// </summary>
    Combat,

    /// <summary>
    /// Enemy's turn (placeholder for future AI implementation).
    /// </summary>
    EnemyTurn
}