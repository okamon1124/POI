using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles phase-related input from UI (View -> Model).
/// Receives button clicks and forwards them to PhaseManager.
/// Does NOT update UI - that's handled by PhaseUIPresenter.
/// 
/// Pattern: INPUT HANDLER
/// - Listens to UI events (button clicks)
/// - Calls Model methods (PhaseManager)
/// - Does NOT modify View directly
/// </summary>
public class PhaseInputHandler : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button endMainPhaseButton;

    private PhaseManager _phaseManager;

    /// <summary>
    /// Initialize with PhaseManager reference.
    /// Call this from GameController after PhaseManager is created.
    /// </summary>
    public void Initialize(PhaseManager phaseManager)
    {
        _phaseManager = phaseManager;

        // Unsubscribe first to avoid duplicate subscriptions
        UnsubscribeFromButtons();
        SubscribeToButtons();
    }

    private void Awake()
    {
        // Fallback subscription if Initialize() not called
        SubscribeToButtons();
    }

    private void OnDestroy()
    {
        UnsubscribeFromButtons();
    }

    private void SubscribeToButtons()
    {
        if (endMainPhaseButton != null)
        {
            endMainPhaseButton.onClick.AddListener(OnEndMainPhaseButtonClicked);
        }
    }

    private void UnsubscribeFromButtons()
    {
        if (endMainPhaseButton != null)
        {
            endMainPhaseButton.onClick.RemoveListener(OnEndMainPhaseButtonClicked);
        }
    }

    // ========== BUTTON HANDLERS ==========

    private void OnEndMainPhaseButtonClicked()
    {
        if (_phaseManager == null)
        {
            Debug.LogWarning("[PhaseInputHandler] PhaseManager is null - cannot end main phase.");
            return;
        }

        // Extra safety check - should be prevented by button being disabled, but just in case
        if (_phaseManager.CurrentPhase != PhaseType.Main)
        {
            // Silently ignore - button shouldn't be clickable anyway
            return;
        }

        // Just forward the input to the model
        _phaseManager.EndMainPhase();

        // Do NOT update UI here - PhaseUIPresenter handles that
    }
}