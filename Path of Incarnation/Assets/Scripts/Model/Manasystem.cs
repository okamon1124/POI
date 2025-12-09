using System;
using UnityEngine;

/// <summary>
/// Model for managing player's mana resources.
/// Tracks current mana, max mana, and provides events for UI updates.
/// </summary>
public class ManaSystem
{
    // ========== EVENTS ==========

    /// <summary>
    /// Fired when current or max mana changes.
    /// Parameters: (currentMana, maxMana)
    /// </summary>
    public event Action<int, int> OnManaChanged;

    /// <summary>
    /// Fired when mana is spent.
    /// Parameter: amount spent
    /// </summary>
    public event Action<int> OnManaSpent;

    /// <summary>
    /// Fired when mana is refreshed (start of turn).
    /// </summary>
    public event Action OnManaRefreshed;

    // ========== STATE ==========

    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    public int AbsoluteMaxMana { get; private set; } = 8; // Can be changed

    public Owner Owner { get; private set; }

    // ========== CONSTRUCTOR ==========

    public ManaSystem(Owner owner, int startingMaxMana = 1, int absoluteMax = 8)
    {
        Owner = owner;
        AbsoluteMaxMana = absoluteMax;
        MaxMana = Mathf.Clamp(startingMaxMana, 0, AbsoluteMaxMana);
        CurrentMana = MaxMana;
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Refresh mana to maximum (typically called at start of player's turn).
    /// </summary>
    public void RefreshMana()
    {
        CurrentMana = MaxMana;
        OnManaRefreshed?.Invoke();
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        //Debug.Log($"[ManaSystem] {Owner} mana refreshed to {CurrentMana}/{MaxMana}");
    }

    /// <summary>
    /// Gain 1 permanent max mana (typically called each turn).
    /// </summary>
    /// <param name="amount">Amount to gain (default 1)</param>
    public void GainMaxMana(int amount = 1)
    {
        int oldMax = MaxMana;
        MaxMana = Mathf.Clamp(MaxMana + amount, 0, AbsoluteMaxMana);

        if (MaxMana != oldMax)
        {
            //Debug.Log($"[ManaSystem] {Owner} max mana: {oldMax} ¡÷ {MaxMana}");
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        }
    }

    /// <summary>
    /// Spend mana. Returns true if successful.
    /// </summary>
    public bool TrySpendMana(int amount, out string reason)
    {
        if (amount < 0)
        {
            reason = "Cannot spend negative mana.";
            return false;
        }

        if (amount > CurrentMana)
        {
            reason = $"Not enough mana. Need {amount}, have {CurrentMana}.";
            return false;
        }

        CurrentMana -= amount;
        OnManaSpent?.Invoke(amount);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] {Owner} spent {amount} mana. Remaining: {CurrentMana}/{MaxMana}");

        reason = null;
        return true;
    }

    /// <summary>
    /// Check if player can afford a cost without spending.
    /// </summary>
    public bool CanAfford(int cost)
    {
        return cost >= 0 && CurrentMana >= cost;
    }

    /// <summary>
    /// Gain temporary mana (e.g., from card effects).
    /// </summary>
    public void GainTemporaryMana(int amount)
    {
        if (amount <= 0) return;

        CurrentMana = Mathf.Min(CurrentMana + amount, MaxMana);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] {Owner} gained {amount} temporary mana. Current: {CurrentMana}/{MaxMana}");
    }

    /// <summary>
    /// Set absolute max mana cap (for game balance changes).
    /// </summary>
    public void SetAbsoluteMaxMana(int newMax)
    {
        AbsoluteMaxMana = Mathf.Max(1, newMax);
        MaxMana = Mathf.Min(MaxMana, AbsoluteMaxMana);
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] {Owner} absolute max mana set to {AbsoluteMaxMana}");
    }

    // ========== DEBUG ==========

    public override string ToString()
    {
        return $"[ManaSystem {Owner}] {CurrentMana}/{MaxMana} (cap: {AbsoluteMaxMana})";
    }
}