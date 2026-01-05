using UnityEngine;

public enum CardType { Creature, Object, Equipment, Environment, Spell }

public enum ZoneType { Hand, Main, Combat, Environment, Equipment, Deployment, Advance }

public enum Owner
{
    None,
    Player,
    Opponent
}

public enum MoveType { Player, System, Effect }

public enum RuleResult
{
    /// <summary>
    /// This rule does not apply to the situation. Continue evaluating other rules.
    /// </summary>
    Ignore,

    /// <summary>
    /// This rule explicitly ALLOWS the move.
    /// Other rules can still deny unless your evaluator short-circuits on Allow.
    /// </summary>
    Allow,

    /// <summary>
    /// This rule blocks the move and provides a reason.
    /// </summary>
    Deny
}