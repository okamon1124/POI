using System;
using UnityEngine;

/// <summary>
/// Service that determines whether card input (dragging) is currently allowed.
/// Listens to PhaseManager and provides a single source of truth for input permissions.
/// 
/// This separates input policy from:
/// - PhaseUIPresenter (which should only handle visuals)
/// - UiCard (which shouldn't need to track phase state)
/// - CardDropInputHandler (which can query this instead of tracking phase itself)
/// </summary>
public class CardInputPolicy
{
    // ----------------- State -----------------

    private PhaseManager _phaseManager;
    private bool _isDraggingAllowed;

    // For tracking drag validity across phase changes
    private int _mainPhaseId;
    private int? _dragStartedInMainPhaseId;

    // ----------------- Events -----------------

    /// <summary>
    /// Fired when drag permission changes. 
    /// UI can optionally listen to update visual hints (like card glow).
    /// </summary>
    public event Action<bool> OnDragPermissionChanged;

    // ----------------- Public API -----------------

    /// <summary>
    /// Can the player currently start dragging a card from hand?
    /// </summary>
    public bool CanStartDrag => _isDraggingAllowed;

    /// <summary>
    /// Was the current drag started in a valid phase, and is it still valid to drop?
    /// Call this when processing a drop to ensure the drag wasn't started in a previous phase.
    /// </summary>
    public bool IsCurrentDragValid => _dragStartedInMainPhaseId.HasValue
                                       && _dragStartedInMainPhaseId.Value == _mainPhaseId;

    // ----------------- Initialization -----------------

    public void Initialize(PhaseManager phaseManager)
    {
        // Unsubscribe from old manager if re-initializing
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered -= OnPhaseEntered;
        }

        _phaseManager = phaseManager;
        _mainPhaseId = 0;
        _dragStartedInMainPhaseId = null;

        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered += OnPhaseEntered;

            // Set initial state
            UpdateDragPermission(_phaseManager.CurrentPhase);
        }
    }

    public void Dispose()
    {
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered -= OnPhaseEntered;
            _phaseManager = null;
        }
    }

    // ----------------- Drag Tracking -----------------

    /// <summary>
    /// Call this when a drag begins to record which phase it started in.
    /// </summary>
    public void NotifyDragStarted()
    {
        if (_isDraggingAllowed)
        {
            _dragStartedInMainPhaseId = _mainPhaseId;
        }
        else
        {
            // Drag started outside valid phase - mark as invalid
            _dragStartedInMainPhaseId = null;
        }
    }

    /// <summary>
    /// Call this when a drag ends to clear tracking state.
    /// </summary>
    public void NotifyDragEnded()
    {
        _dragStartedInMainPhaseId = null;
    }

    // ----------------- Private -----------------

    private void OnPhaseEntered(PhaseType phase)
    {
        if (phase == PhaseType.Main)
        {
            _mainPhaseId++;
        }

        UpdateDragPermission(phase);
    }

    private void UpdateDragPermission(PhaseType phase)
    {
        bool wasAllowed = _isDraggingAllowed;
        _isDraggingAllowed = (phase == PhaseType.Main);

        if (wasAllowed != _isDraggingAllowed)
        {
            OnDragPermissionChanged?.Invoke(_isDraggingAllowed);
        }
    }
}