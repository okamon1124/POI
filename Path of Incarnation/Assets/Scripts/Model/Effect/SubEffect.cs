using Coffee.UIEffects;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Base class for all sub-effects (atomic actions in an effect).
/// Each SubEffect performs one action: deal damage, draw cards, destroy, etc.
/// 
/// SubEffects are ScriptableObjects so they can be created in the editor
/// and referenced by Effect assets.
/// </summary>
public abstract class SubEffect : ScriptableObject
{
    [Header("Whiteboard")]
    [Tooltip("Key to write results to. Leave empty if this effect doesn't produce output.")]
    [SerializeField] protected string writeToKey;

    /// <summary>
    /// Execute this sub-effect.
    /// </summary>
    /// <param name="context">The effect context containing targets and whiteboard.</param>
    /// <returns>True if the effect executed successfully, false if it failed.</returns>
    public abstract UniTask<bool> Execute(EffectContext context);

    /// <summary>
    /// Check if this effect can execute with the current context.
    /// Override to add custom validation beyond target validation.
    /// </summary>
    public virtual bool CanExecute(EffectContext context)
    {
        return context != null && !context.IsCancelled;
    }

    /// <summary>
    /// Get a human-readable description of this effect.
    /// Used for UI tooltips and logging.
    /// </summary>
    public abstract string GetDescription();

    /// <summary>
    /// Helper to store results in whiteboard if writeToKey is set.
    /// </summary>
    protected void StoreResult<T>(EffectContext context, T result)
    {
        if (!string.IsNullOrEmpty(writeToKey))
        {
            context.Store(writeToKey, result);
        }
    }

    /// <summary>
    /// Helper to append to a list result in whiteboard if writeToKey is set.
    /// </summary>
    protected void AppendResult<T>(EffectContext context, T result)
    {
        if (!string.IsNullOrEmpty(writeToKey))
        {
            context.AppendToList(writeToKey, result);
        }
    }
}