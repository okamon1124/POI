using UnityEngine;
using System.Collections.Generic;

public class ZoneHighlighter : MonoBehaviour
{
    // current valid zones for the dragged card
    private readonly HashSet<UiZone> _valid = new HashSet<UiZone>();
    private UiCard _dragging = null;
    private UiZone _hot = null; // the currently hovered valid zone (while dragging)

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDragChangedEvent>(OnDragChanged);
        EventBus.Subscribe<ZonePointerEnterEvent>(OnZoneEnter);
        EventBus.Subscribe<ZonePointerExitEvent>(OnZoneExit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDragChangedEvent>(OnDragChanged);
        EventBus.Unsubscribe<ZonePointerEnterEvent>(OnZoneEnter);
        EventBus.Unsubscribe<ZonePointerExitEvent>(OnZoneExit);
    }

    private void OnDragChanged(PlayerDragChangedEvent e)
    {
        // clear previous visuals
        if (_hot) { _hot.SetHighlightLevel(ZoneHighlightLevel.Available); _hot = null; }
        foreach (var z in _valid) z.SetHighlightLevel(ZoneHighlightLevel.Off);
        _valid.Clear();

        _dragging = e.Dragged;
        if (_dragging == null) return;

        // mark all valid zones as "Available"
        var valid = ZoneManager.I.GetValidDestinations(_dragging);
        foreach (var zone in valid)
        {
            _valid.Add(zone);
            zone.SetHighlightLevel(ZoneHighlightLevel.Available);
        }
    }

    private void OnZoneEnter(ZonePointerEnterEvent e)
    {
        // only react during a drag & only for valid zones
        if (_dragging == null) return;
        var z = e.Zone;
        if (!_valid.Contains(z)) return;

        // (optional live re-check if capacity/rules can change mid-drag)
        if (!MoveRules.CanMoveZoneToZone(_dragging, _dragging.CurrentZone, z, MoveType.Player, out _))
            return;

        // demote previously hot zone
        if (_hot && _hot != z && _valid.Contains(_hot))
            _hot.SetHighlightLevel(ZoneHighlightLevel.Available);

        // promote current zone
        _hot = z;
        _hot.SetHighlightLevel(ZoneHighlightLevel.Hot);
    }

    private void OnZoneExit(ZonePointerExitEvent e)
    {
        if (_dragging == null) return;
        var z = e.Zone;
        if (!_valid.Contains(z)) return;

        if (_hot == z)
        {
            _hot.SetHighlightLevel(ZoneHighlightLevel.Available);
            _hot = null;
        }
    }
}
