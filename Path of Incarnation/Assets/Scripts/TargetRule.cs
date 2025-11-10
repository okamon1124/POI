using UnityEngine;

public enum TargetResult { Allow, Deny, Ignore }

public sealed class TargetContext
{
    public UiCard caster;   // the spell card
    public UiCard target;   // a unit/equipment/etc. on board (can be null for zone)
    public Zone zone;     // optional: zone target
}

public abstract class TargetRule : ScriptableObject
{
    public abstract TargetResult Evaluate(TargetContext ctx, out string reason);
}