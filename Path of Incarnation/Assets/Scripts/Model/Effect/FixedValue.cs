using Coffee.UIEffects;
using System;
using UnityEngine;

/// <summary>
/// A fixed integer value for effects.
/// Use this for simple effects like "Deal 3 damage" or "Draw 2 cards".
/// </summary>
[Serializable]
public class FixedValue : IEffectValue
{
    [SerializeField] private int value;

    public FixedValue() { }

    public FixedValue(int value)
    {
        this.value = value;
    }

    public int Evaluate(EffectContext context)
    {
        return value;
    }

    /// <summary>
    /// Raw value accessor for editor/inspector use.
    /// </summary>
    public int RawValue
    {
        get => value;
        set => this.value = value;
    }

    public override string ToString()
    {
        return value.ToString();
    }
}