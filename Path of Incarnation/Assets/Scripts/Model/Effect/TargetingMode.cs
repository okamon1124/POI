/// <summary>
/// Defines how targets are selected for an effect step.
/// </summary>
public enum TargetingMode
{
    /// <summary>
    /// No selection needed. Used with TargetType.None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Player manually chooses target(s) from valid options.
    /// UI will highlight valid targets and wait for selection.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Automatically selects ALL valid targets.
    /// Used for AoE effects like "Deal 1 damage to all enemy creatures".
    /// </summary>
    All = 2,

    /// <summary>
    /// Randomly selects from valid targets.
    /// Used for effects like "Deal 3 damage to a random enemy".
    /// </summary>
    Random = 3,

    /// <summary>
    /// Targets the card/player that owns/cast this effect.
    /// Used for self-buff or "Draw a card" type effects.
    /// </summary>
    Self = 4,

    /// <summary>
    /// Targets the source card that triggered this effect.
    /// Used primarily for triggered abilities.
    /// </summary>
    Source = 5,

    /// <summary>
    /// Read targets from whiteboard storage.
    /// Used for chained effects like "Draw 2, then discard 1 of them".
    /// </summary>
    FromWhiteboard = 6,
}