/// <summary>
/// Interface for any game object that can be targeted by effects.
/// Implemented by: CardInstance, Slot, PlayerState
/// 
/// This is a pure model-side interface. For UI concerns (highlighting, positions),
/// use UiRegistry to look up the corresponding view (UiCard, UiSlot, etc.).
/// </summary>
public interface ITargetable
{
    /// <summary>
    /// What type of target this is (for filtering by TargetType).
    /// </summary>
    TargetType TargetType { get; }

    /// <summary>
    /// Who owns this target.
    /// </summary>
    Owner Owner { get; }

    /// <summary>
    /// Is this target still valid? (Not destroyed, not removed from play, etc.)
    /// Effects should check this before applying.
    /// </summary>
    bool IsValidTarget { get; }

    /// <summary>
    /// Display name for UI and logging purposes.
    /// </summary>
    string DisplayName { get; }
}