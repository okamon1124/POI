using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Presents phase state changes to the UI (Model -> View).
/// Listens to PhaseManager events and updates visual elements.
/// 
/// ONLY handles visual presentation:
/// - Phase text display
/// - Turn counter
/// - Phase banners
/// - Button states (visual only)
/// 
/// Does NOT handle input - that's CardInputPolicy + CardDropInputHandler.
/// </summary>
public class PhaseUIPresenter : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button endMainPhaseButton;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text turnCounterText;
    [SerializeField] private GameObject phaseBanner;
    [SerializeField] private TMP_Text phaseBannerText;

    [Header("Banner CanvasGroup (for fade)")]
    [SerializeField] private CanvasGroup phaseBannerCanvasGroup;

    [Header("Button Text Config")]
    [SerializeField] private string mainPhaseButtonText = "End Main Phase";
    [SerializeField] private string disabledButtonText = "Waiting...";

    [Header("Phase Banner Timing")]
    [SerializeField] private float bannerFadeInDuration = 0.25f;
    [SerializeField] private float bannerVisibleDuration = 1.5f;
    [SerializeField] private float bannerFadeOutDuration = 0.25f;

    [Header("Phase Banner Visibility")]
    [SerializeField] private bool showDrawPhaseBanner = false;
    [SerializeField] private bool showMainPhaseBanner = false;
    [SerializeField] private bool showMovementPhaseBanner = true;
    [SerializeField] private bool showCombatPhaseBanner = true;
    [SerializeField] private bool showEnemyTurnBanner = false;

    private PhaseManager _phaseManager;
    private Sequence _bannerSequence;

    // ========== INITIALIZATION ==========

    private void Awake()
    {
        // Ensure we have a CanvasGroup on the banner for fading
        if (phaseBanner != null && phaseBannerCanvasGroup == null)
        {
            phaseBannerCanvasGroup = phaseBanner.GetComponent<CanvasGroup>();
            if (phaseBannerCanvasGroup == null)
                phaseBannerCanvasGroup = phaseBanner.AddComponent<CanvasGroup>();
        }

        // Start hidden
        if (phaseBanner != null && phaseBannerCanvasGroup != null)
        {
            phaseBannerCanvasGroup.alpha = 0f;
            phaseBanner.SetActive(false);
        }
    }

    /// <summary>
    /// Initialize with PhaseManager to listen to its events.
    /// Call this from GameController after PhaseManager is created.
    /// </summary>
    public void Initialize(PhaseManager phaseManager)
    {
        if (_phaseManager != null)
        {
            UnsubscribeFromPhaseManager();
        }

        _phaseManager = phaseManager;

        if (_phaseManager != null)
        {
            SubscribeToPhaseManager();

            // Initialize UI to current state
            UpdateButtonState(_phaseManager.CurrentPhase);
            UpdatePhaseDisplay(_phaseManager.CurrentPhase);
            UpdateTurnCounter(_phaseManager.TurnNumber);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromPhaseManager();
        KillBannerSequence();
    }

    private void SubscribeToPhaseManager()
    {
        _phaseManager.OnPhaseEntered += OnPhaseEntered;
        _phaseManager.OnPhaseExited += OnPhaseExited;
        _phaseManager.OnTurnCompleted += OnTurnCompleted;
    }

    private void UnsubscribeFromPhaseManager()
    {
        if (_phaseManager != null)
        {
            _phaseManager.OnPhaseEntered -= OnPhaseEntered;
            _phaseManager.OnPhaseExited -= OnPhaseExited;
            _phaseManager.OnTurnCompleted -= OnTurnCompleted;
        }
    }

    // ========== MODEL EVENTS (PhaseManager) ==========

    private void OnPhaseEntered(PhaseType phase)
    {
        UpdateButtonState(phase);
        UpdatePhaseDisplay(phase);
        ShowPhaseBanner(phase);

        // NOTE: Drag permission is now handled by CardInputPolicy
        // This presenter only updates visuals
    }

    private void OnPhaseExited(PhaseType phase)
    {
        // Optional: exit animations here if needed
    }

    private void OnTurnCompleted(int completedTurnNumber)
    {
        // Show next turn number
        UpdateTurnCounter(completedTurnNumber + 1);
    }

    // ========== VIEW UPDATES ==========

    private void UpdateButtonState(PhaseType phase)
    {
        if (endMainPhaseButton == null) return;

        bool isMainPhase = (phase == PhaseType.Main);

        endMainPhaseButton.interactable = isMainPhase;

        var buttonText = endMainPhaseButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isMainPhase ? mainPhaseButtonText : disabledButtonText;
        }
    }

    private void UpdatePhaseDisplay(PhaseType phase)
    {
        if (phaseText == null) return;

        phaseText.text = GetPhaseDisplayName(phase);
    }

    private void UpdateTurnCounter(int turnNumber)
    {
        if (turnCounterText != null)
        {
            turnCounterText.text = $"Turn {turnNumber}";
        }
    }

    // ========== BANNER LOGIC ==========

    private void ShowPhaseBanner(PhaseType phase)
    {
        if (phaseBanner == null || phaseBannerText == null || phaseBannerCanvasGroup == null)
            return;

        // Check if we should show banner for this phase
        bool shouldShow = phase switch
        {
            PhaseType.Draw => showDrawPhaseBanner,
            PhaseType.Main => showMainPhaseBanner,
            PhaseType.Movement => showMovementPhaseBanner,
            PhaseType.Combat => showCombatPhaseBanner,
            PhaseType.EnemyTurn => showEnemyTurnBanner,
            _ => false
        };

        if (!shouldShow)
        {
            HidePhaseBannerImmediate();
            return;
        }

        phaseBannerText.text = GetPhaseBannerText(phase);

        // Ensure active & start from invisible
        phaseBanner.SetActive(true);
        phaseBannerCanvasGroup.alpha = 0f;

        // Kill any previous animation
        KillBannerSequence();

        // Build a new sequence: fade in -> hold -> fade out
        _bannerSequence = DOTween.Sequence()
            .Append(phaseBannerCanvasGroup.DOFade(1f, bannerFadeInDuration))
            .AppendInterval(bannerVisibleDuration)
            .Append(phaseBannerCanvasGroup.DOFade(0f, bannerFadeOutDuration))
            .OnComplete(() =>
            {
                phaseBanner.SetActive(false);
            });
    }

    private void HidePhaseBannerImmediate()
    {
        if (phaseBanner == null || phaseBannerCanvasGroup == null)
            return;

        KillBannerSequence();
        phaseBannerCanvasGroup.alpha = 0f;
        phaseBanner.SetActive(false);
    }

    private void KillBannerSequence()
    {
        if (_bannerSequence != null)
        {
            _bannerSequence.Kill();
            _bannerSequence = null;
        }
    }

    // ========== HELPERS ==========

    private string GetPhaseDisplayName(PhaseType phase)
    {
        return phase switch
        {
            PhaseType.Draw => "Draw Phase",
            PhaseType.Main => "Main Phase",
            PhaseType.Movement => "Movement Phase",
            PhaseType.Combat => "Combat Phase",
            PhaseType.EnemyTurn => "Enemy Turn",
            _ => phase.ToString()
        };
    }

    private string GetPhaseBannerText(PhaseType phase)
    {
        return phase switch
        {
            PhaseType.Draw => "DRAW PHASE",
            PhaseType.Main => "MAIN PHASE",
            PhaseType.Movement => "MOVEMENT PHASE",
            PhaseType.Combat => "COMBAT!",
            PhaseType.EnemyTurn => "ENEMY TURN",
            _ => GetPhaseDisplayName(phase).ToUpper()
        };
    }

    // ========== EDITOR DEBUG (optional) ==========

#if UNITY_EDITOR
    [ContextMenu("Test Banner: Draw")]
    private void TestBannerDraw() => ShowPhaseBanner(PhaseType.Draw);

    [ContextMenu("Test Banner: Main")]
    private void TestBannerMain() => ShowPhaseBanner(PhaseType.Main);

    [ContextMenu("Test Banner: Movement")]
    private void TestBannerMovement() => ShowPhaseBanner(PhaseType.Movement);

    [ContextMenu("Test Banner: Combat")]
    private void TestBannerCombat() => ShowPhaseBanner(PhaseType.Combat);

    [ContextMenu("Test Banner: Enemy")]
    private void TestBannerEnemy() => ShowPhaseBanner(PhaseType.EnemyTurn);
#endif
}