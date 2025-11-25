using System.Collections.Generic;
using UnityEngine;

public class SlotHighlightPresenter : MonoBehaviour
{
    private readonly HashSet<UiSlot> _valid = new();
    private UiSlot _hot;

    private Board board;
    private UiRegistry uiBoard;

    public void Initialize(Board boardController, UiRegistry uiBoardRef)
    {
        board = boardController;
        uiBoard = uiBoardRef;

        EventBus.Subscribe<PlayerDragChangedEvent>(OnDragChanged);
        EventBus.Subscribe<SlotPointerEnterEvent>(OnSlotEnter);
        EventBus.Subscribe<SlotPointerExitEvent>(OnSlotExit);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PlayerDragChangedEvent>(OnDragChanged);
        EventBus.Unsubscribe<SlotPointerEnterEvent>(OnSlotEnter);
        EventBus.Unsubscribe<SlotPointerExitEvent>(OnSlotExit);
    }

    private UiCard CurrentDragging => PlayerCardInputState.I != null
        ? PlayerCardInputState.I.DraggingCard
        : null;

    private void OnDragChanged(PlayerDragChangedEvent e)
    {
        // clear old highlights
        if (_hot != null)
        {
            _hot.SetHighlightLevel(SlotHighlightLevel.Available);
            _hot = null;
        }
        foreach (var s in _valid)
            s.SetHighlightLevel(SlotHighlightLevel.Off);
        _valid.Clear();

        var dragging = e.Dragged;
        if (dragging == null || dragging.cardInstance == null || board == null || uiBoard == null)
            return;

        var instance = dragging.cardInstance;
        var fromSlot = instance.CurrentSlot;
        if (fromSlot == null) return;

        // compute all valid destination slots
        foreach (var zone in board.Zones)
        {
            foreach (var slot in zone.Slots)
            {
                if (slot == fromSlot) continue;

                if (!MoveRules.CanMove(instance, fromSlot, slot, MoveType.Player, out _))
                    continue;

                var uiSlot = uiBoard.GetUiSlot(slot);
                if (uiSlot == null) continue;

                _valid.Add(uiSlot);
                uiSlot.SetHighlightLevel(SlotHighlightLevel.Available);
            }
        }
    }

    private void OnSlotEnter(SlotPointerEnterEvent e)
    {
        var dragging = CurrentDragging;
        if (dragging == null) return;

        var uiSlot = e.Slot;
        if (uiSlot == null || !_valid.Contains(uiSlot))
            return;

        var instance = dragging.cardInstance;
        var from = instance?.CurrentSlot;
        var to = uiSlot.ModelSlot;
        if (instance == null || from == null || to == null)
            return;

        // optional re-check to be safe
        if (!MoveRules.CanMove(instance, from, to, MoveType.Player, out _))
            return;

        if (_hot != null && _hot != uiSlot && _valid.Contains(_hot))
            _hot.SetHighlightLevel(SlotHighlightLevel.Available);

        _hot = uiSlot;
        _hot.SetHighlightLevel(SlotHighlightLevel.Hot);
    }

    private void OnSlotExit(SlotPointerExitEvent e)
    {
        var dragging = CurrentDragging;
        if (dragging == null) return;

        var uiSlot = e.Slot;
        if (uiSlot == null || !_valid.Contains(uiSlot))
            return;

        if (_hot == uiSlot)
        {
            _hot.SetHighlightLevel(SlotHighlightLevel.Available);
            _hot = null;
        }
    }
}
