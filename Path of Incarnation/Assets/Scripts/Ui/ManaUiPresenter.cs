using UnityEngine;

/// <summary>
/// Presenter that syncs ManaSystem model with ManaOrb visuals.
/// Listens to mana changes and player hover events to update orb states.
/// Pattern: PRESENTER (Model -> View)
/// </summary>
public class ManaUIPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ManaOrb[] manaOrbs; // Should match AbsoluteMaxMana (8)

    private ManaSystem _manaSystem;
    private int _previewCost = 0;
    private bool _isPreviewActive = false;
    private bool _isDragging = false;

    // ========== INITIALIZATION ==========

    public void Initialize(ManaSystem manaSystem)
    {
        if (_manaSystem != null)
        {
            UnsubscribeFromManaSystem();
        }

        _manaSystem = manaSystem;

        if (_manaSystem != null)
        {
            SubscribeToManaSystem();
            UpdateAllOrbs(); // Initial state
        }

        // Subscribe to hover and drag events
        EventBus.Subscribe<PlayerHoverChangedEvent>(OnPlayerHoverChanged);
        EventBus.Subscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Subscribe<CardDragEndEvent>(OnDragEnd);
    }

    private void OnDestroy()
    {
        UnsubscribeFromManaSystem();
        EventBus.Unsubscribe<PlayerHoverChangedEvent>(OnPlayerHoverChanged);
        EventBus.Unsubscribe<CardDragBeginEvent>(OnDragBegin);
        EventBus.Unsubscribe<CardDragEndEvent>(OnDragEnd);
    }

    private void SubscribeToManaSystem()
    {
        _manaSystem.OnManaChanged += OnManaChanged;
        _manaSystem.OnManaSpent += OnManaSpent;
        _manaSystem.OnManaRefreshed += OnManaRefreshed;
    }

    private void UnsubscribeFromManaSystem()
    {
        if (_manaSystem != null)
        {
            _manaSystem.OnManaChanged -= OnManaChanged;
            _manaSystem.OnManaSpent -= OnManaSpent;
            _manaSystem.OnManaRefreshed -= OnManaRefreshed;
        }
    }

    // ========== MANA SYSTEM EVENTS ==========

    private void OnManaChanged(int currentMana, int maxMana)
    {
        UpdateAllOrbs();
    }

    private void OnManaSpent(int amount)
    {
        // Orbs already updated via OnManaChanged
    }

    private void OnManaRefreshed()
    {
        // Orbs already updated via OnManaChanged
    }

    // ========== DRAG EVENTS ==========

    private void OnDragBegin(CardDragBeginEvent e)
    {
        _isDragging = true;
    }

    private void OnDragEnd(CardDragEndEvent e)
    {
        _isDragging = false;
    }

    // ========== HOVER EVENTS ==========

    private void OnPlayerHoverChanged(PlayerHoverChangedEvent e)
    {
        // Ignore hover changes while dragging to prevent preview flickering
        if (_isDragging) return;

        if (e.Hovered == null)
        {
            // No longer hovering
            ClearPreview();
        }
        else
        {
            // Hovering over a card - show cost preview
            int cost = e.Hovered.cardInstance?.Data?.manaCost ?? 0;
            ShowPreview(cost);
        }
    }

    // ========== PREVIEW LOGIC ==========

    private void ShowPreview(int cost)
    {
        if (_manaSystem == null) return;

        bool costChanged = (_previewCost != cost);

        _previewCost = Mathf.Max(0, cost);
        _isPreviewActive = _previewCost > 0;

        // When cost changes, kill breathing so new preview orbs all restart in sync
        if (costChanged && manaOrbs != null)
        {
            for (int i = 0; i < manaOrbs.Length; i++)
            {
                if (manaOrbs[i] != null)
                {
                    manaOrbs[i].StopBreathingImmediate();
                }
            }
        }

        UpdateAllOrbs();
    }

    private void ClearPreview()
    {
        if (!_isPreviewActive) return;

        _isPreviewActive = false;
        _previewCost = 0;

        UpdateAllOrbs();
    }

    // ========== ORB UPDATE LOGIC ==========

    private void UpdateAllOrbs()
    {
        if (_manaSystem == null || manaOrbs == null) return;

        int currentMana = _manaSystem.CurrentMana;
        int maxMana = _manaSystem.MaxMana; // kept in case you need it later

        for (int i = 0; i < manaOrbs.Length; i++)
        {
            if (manaOrbs[i] == null) continue;

            ManaOrb.OrbState state = GetOrbState(i, currentMana, maxMana);
            manaOrbs[i].SetState(state);
        }
    }

    private ManaOrb.OrbState GetOrbState(int orbIndex, int currentMana, int maxMana)
    {
        // PREVIEW MODE: decide from previewCost + currentMana
        if (_isPreviewActive && _previewCost > 0)
        {
            // Inside the preview cost range
            if (orbIndex < _previewCost)
            {
                // This orb corresponds to mana you actually have
                if (orbIndex < currentMana)
                {
                    return ManaOrb.OrbState.PreviewAffordable;
                }

                // This orb is part of the cost but you DON'T have this mana yet
                return ManaOrb.OrbState.PreviewUnaffordable;
            }

            // Outside preview cost: fall back to normal state (available/spent)
            if (orbIndex < currentMana)
            {
                return ManaOrb.OrbState.Available;
            }

            return ManaOrb.OrbState.Spent;
        }

        // NORMAL MODE (no preview):
        if (orbIndex < currentMana)
        {
            return ManaOrb.OrbState.Available;
        }

        return ManaOrb.OrbState.Spent;
    }

    // ========== DEBUG HELPERS ==========

#if UNITY_EDITOR
    [ContextMenu("Test: Full Mana")]
    private void TestFullMana()
    {
        if (_manaSystem != null)
        {
            _manaSystem.RefreshMana();
        }
    }

    [ContextMenu("Test: Spend 3 Mana")]
    private void TestSpend3()
    {
        if (_manaSystem != null)
        {
            _manaSystem.TrySpendMana(3, out _);
        }
    }

    [ContextMenu("Test: Preview 5 Cost")]
    private void TestPreview()
    {
        ShowPreview(5);
        Invoke(nameof(ClearPreview), 2f);
    }
#endif
}