using Coffee.UIEffects;

/// <summary>
/// Interface for values used in effects.
/// Allows for fixed values now, with future support for dynamic values
/// (e.g., "damage equal to hand size", "heal equal to damage dealt").
/// </summary>
public interface IEffectValue
{
    /// <summary>
    /// Evaluate and return the integer value.
    /// </summary>
    /// <param name="context">The effect context, used for dynamic calculations.</param>
    /// <returns>The calculated value.</returns>
    int Evaluate(EffectContext context);
}