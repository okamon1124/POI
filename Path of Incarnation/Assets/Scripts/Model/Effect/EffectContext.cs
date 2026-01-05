using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime context (whiteboard) for effect resolution.
/// Created fresh for each effect, tracks targets, results, and shared state between steps.
/// </summary>
public class EffectContext
{
    // ========================= Identity =========================

    /// <summary>
    /// The card that created/cast this effect. Can be null for system effects.
    /// </summary>
    public CardInstance Source { get; }

    /// <summary>
    /// Who owns/triggered this effect (the player casting the spell).
    /// </summary>
    public Owner Owner { get; }

    /// <summary>
    /// Reference to the board for targeting and game state queries.
    /// </summary>
    public Board Board { get; }

    /// <summary>
    /// Reference to the caster's PlayerState.
    /// </summary>
    public PlayerState OwnerState { get; }

    /// <summary>
    /// Reference to the opponent's PlayerState.
    /// </summary>
    public PlayerState OpponentState { get; }

    /// <summary>
    /// Reference to the caster's Deck. Can be null if not applicable.
    /// </summary>
    public Deck OwnerDeck { get; }

    /// <summary>
    /// Reference to the opponent's Deck. Can be null if not applicable.
    /// </summary>
    public Deck OpponentDeck { get; }

    // ========================= Current Step State =========================

    /// <summary>
    /// Targets selected for the current step.
    /// Cleared and repopulated for each step.
    /// </summary>
    public List<ITargetable> CurrentTargets { get; } = new();

    /// <summary>
    /// Index of the currently executing step (0-based).
    /// </summary>
    public int CurrentStepIndex { get; set; }

    // ========================= Whiteboard Storage =========================

    private readonly Dictionary<string, object> _storage = new();

    // ========================= Resolution State =========================

    /// <summary>
    /// Set to true if the effect should stop resolving (e.g., all targets invalid).
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Reason for cancellation, if any.
    /// </summary>
    public string CancellationReason { get; set; }

    // ========================= Constructor =========================

    public EffectContext(
        CardInstance source,
        Owner owner,
        Board board,
        PlayerState ownerState,
        PlayerState opponentState,
        Deck ownerDeck = null,
        Deck opponentDeck = null)
    {
        Source = source;
        Owner = owner;
        Board = board;
        OwnerState = ownerState;
        OpponentState = opponentState;
        OwnerDeck = ownerDeck;
        OpponentDeck = opponentDeck;
        CurrentStepIndex = 0;
        IsCancelled = false;
    }

    // ========================= Whiteboard API =========================

    /// <summary>
    /// Store a value in the whiteboard for later steps to access.
    /// </summary>
    public void Store<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[EffectContext] Attempted to store with null/empty key.");
            return;
        }

        _storage[key] = value;
    }

    /// <summary>
    /// Retrieve a value from the whiteboard.
    /// Returns default(T) if key not found or type mismatch.
    /// </summary>
    public T Retrieve<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            return default;

        if (_storage.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            Debug.LogWarning($"[EffectContext] Type mismatch for key '{key}'. " +
                           $"Expected {typeof(T).Name}, got {value?.GetType().Name ?? "null"}");
        }

        return default;
    }

    /// <summary>
    /// Check if a key exists in the whiteboard.
    /// </summary>
    public bool HasKey(string key)
    {
        return !string.IsNullOrEmpty(key) && _storage.ContainsKey(key);
    }

    /// <summary>
    /// Try to retrieve a value from the whiteboard.
    /// </summary>
    public bool TryRetrieve<T>(string key, out T value)
    {
        value = default;

        if (string.IsNullOrEmpty(key))
            return false;

        if (_storage.TryGetValue(key, out var stored) && stored is T typedValue)
        {
            value = typedValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Append a value to a list in the whiteboard.
    /// Creates the list if it doesn't exist.
    /// </summary>
    public void AppendToList<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (!_storage.TryGetValue(key, out var existing))
        {
            _storage[key] = new List<T> { value };
        }
        else if (existing is List<T> list)
        {
            list.Add(value);
        }
        else
        {
            Debug.LogWarning($"[EffectContext] Key '{key}' exists but is not a List<{typeof(T).Name}>.");
        }
    }

    /// <summary>
    /// Retrieve a list from the whiteboard, or empty list if not found.
    /// </summary>
    public List<T> RetrieveList<T>(string key)
    {
        if (TryRetrieve<List<T>>(key, out var list))
            return list;

        return new List<T>();
    }

    // ========================= Targeting Helpers =========================

    /// <summary>
    /// Clear current targets for a new step.
    /// </summary>
    public void ClearCurrentTargets()
    {
        CurrentTargets.Clear();
    }

    /// <summary>
    /// Add a target to the current step's target list.
    /// </summary>
    public void AddTarget(ITargetable target)
    {
        if (target != null && !CurrentTargets.Contains(target))
        {
            CurrentTargets.Add(target);
        }
    }

    /// <summary>
    /// Get current targets cast to a specific type.
    /// Filters out any that don't match the type.
    /// </summary>
    public List<T> GetTargetsAs<T>() where T : class, ITargetable
    {
        var result = new List<T>();
        foreach (var target in CurrentTargets)
        {
            if (target is T typed)
                result.Add(typed);
        }
        return result;
    }

    /// <summary>
    /// Get the first target cast to a specific type, or null.
    /// </summary>
    public T GetFirstTargetAs<T>() where T : class, ITargetable
    {
        foreach (var target in CurrentTargets)
        {
            if (target is T typed)
                return typed;
        }
        return null;
    }

    // ========================= Utility =========================

    /// <summary>
    /// Cancel the effect with a reason.
    /// </summary>
    public void Cancel(string reason)
    {
        IsCancelled = true;
        CancellationReason = reason;
        Debug.Log($"[EffectContext] Effect cancelled: {reason}");
    }

    /// <summary>
    /// Get PlayerState for a given owner.
    /// </summary>
    public PlayerState GetPlayerState(Owner owner)
    {
        return owner == Owner ? OwnerState : OpponentState;
    }

    /// <summary>
    /// Get Deck for a given owner.
    /// </summary>
    public Deck GetDeck(Owner owner)
    {
        return owner == Owner ? OwnerDeck : OpponentDeck;
    }

    /// <summary>
    /// Get the opponent of the effect owner.
    /// </summary>
    public Owner GetOpponentOwner()
    {
        return Owner == Owner.Player ? Owner.Opponent : Owner.Player;
    }
}