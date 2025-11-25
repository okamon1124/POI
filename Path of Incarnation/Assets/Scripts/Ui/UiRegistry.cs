using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 純粹的 Model ↔ UI 映射中心：
/// - Slot -> UiSlot
/// - CardInstance -> UiCard
/// 不訂 BoardController 事件，不做動畫。
/// </summary>
public class UiRegistry : MonoBehaviour
{
    private readonly Dictionary<Slot, UiSlot> slotToView = new();
    private readonly Dictionary<CardInstance, UiCard> cardToView = new();

    /// <summary>
    /// 如果你需要遍歷全部 UiSlot，可以用這個。
    /// </summary>
    public IEnumerable<UiSlot> AllUiSlots => slotToView.Values;

    // -------- Registration --------

    public void Register(UiSlot uiSlot)
    {
        if (uiSlot != null && uiSlot.ModelSlot != null)
        {
            slotToView[uiSlot.ModelSlot] = uiSlot;
        }
    }

    public void Register(UiCard uiCard)
    {
        if (uiCard != null && uiCard.cardInstance != null)
        {
            cardToView[uiCard.cardInstance] = uiCard;
        }
    }

    // 可選：如果 UiCard / UiSlot OnDestroy 時呼叫，可以避免殘留引用
    public void Unregister(UiSlot uiSlot)
    {
        if (uiSlot != null && uiSlot.ModelSlot != null)
        {
            if (slotToView.TryGetValue(uiSlot.ModelSlot, out var existing) &&
                existing == uiSlot)
            {
                slotToView.Remove(uiSlot.ModelSlot);
            }
        }
    }

    public void Unregister(UiCard uiCard)
    {
        if (uiCard != null && uiCard.cardInstance != null)
        {
            if (cardToView.TryGetValue(uiCard.cardInstance, out var existing) &&
                existing == uiCard)
            {
                cardToView.Remove(uiCard.cardInstance);
            }
        }
    }

    // -------- Queries --------

    public UiSlot GetUiSlot(Slot slot)
    {
        if (slot == null) return null;
        return slotToView.TryGetValue(slot, out var view) ? view : null;
    }

    public UiCard GetUiCard(CardInstance instance)
    {
        if (instance == null) return null;
        return cardToView.TryGetValue(instance, out var view) ? view : null;
    }

    public IEnumerable<UiCard> GetUiCardsByOwner(Owner owner)
    {
        foreach (var kv in cardToView)
        {
            var instance = kv.Key;
            var uiCard = kv.Value;

            if (instance != null && instance.Owner == owner)
                yield return uiCard;
        }
    }
}