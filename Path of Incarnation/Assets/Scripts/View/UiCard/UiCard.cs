using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using static UiCardStateDefinitions;
using State = StateMachine<UiCard>.State;

/// <summary>
/// Main coordinator for card UI behavior.
/// Delegates to specialized components: UiCardAnimator, UiCardVisuals, and state machine.
/// Handles pointer events and coordinates between logic (CardInstance) and presentation.
/// </summary>
public class UiCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region Components & References

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private StateMachine<UiCard> stateMachine;
    private UiCardAnimator animator;

    [SerializeField] private UiCardVisuals visuals;
    [SerializeField] private UiCardConfig config;

    private UiRegistry uiRegistry;

    // Input policy (injected) - single source of truth for drag permissions
    private CardInputPolicy inputPolicy;

    #endregion

    #region Logic Link

    [Header("Logic Link")]
    public CardInstance cardInstance;
    public Owner Owner => cardInstance?.Owner ?? Owner.None;

    #endregion

    #region State

    private Vector3 initialScale;
    private bool isPointerOver;
    private bool isInteractable = true;

    #endregion

    #region Events Enum

    private enum Event : int
    {
        PointerEnter = 0,
        PointerExit = 1,
        PointerUp = 2,
        PointerDown = 3,
        BeginDrag = 4,
        EndDrag = 5,
        ReturnedToSlot = 6,
        ReturnedToHand = 7,
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetOrAddComponent<CanvasGroup>();

        if (!visuals)
            visuals = GetComponent<UiCardVisuals>();

        ValidateSetup();
        InitializeComponents();
    }

    private void Start()
    {
        initialScale = rectTransform.localScale;
        SetupStateMachine();
    }

    private void Update()
    {
        stateMachine?.Update();
    }

    private void OnDestroy()
    {
        // Stop all tweens on this object to avoid callbacks after destroy
        DOTween.Kill(rectTransform);
        if (visuals?.Visual != null)
            DOTween.Kill(visuals.Visual);
    }

    #endregion

    #region Initialization

    private void ValidateSetup()
    {
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            Debug.LogWarning($"[UiCard:{name}] WorldSpace Canvas has no camera set. Drag position may be off.");
        }

        if (config == null)
        {
            Debug.LogError($"[UiCard:{name}] UiCardConfig is missing! Please assign in inspector.");
        }

        if (visuals == null)
        {
            Debug.LogError($"[UiCard:{name}] UiCardVisuals component is missing!");
        }
        else if (visuals.Visual == null)
        {
            Debug.LogWarning($"[UiCard:{name}] UiCardVisuals.Visual (RectTransform) is not assigned! Hand hover lift won't work.");
        }
    }

    private void InitializeComponents()
    {
        animator = new UiCardAnimator(rectTransform, visuals?.Visual, config);
        visuals?.Initialize(config);
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        return GetComponent<T>() ?? gameObject.AddComponent<T>();
    }

    #endregion

    #region State Machine Setup

    private void SetupStateMachine()
    {
        stateMachine = new StateMachine<UiCard>(this);

        SetupSlotTransitions();
        SetupHandTransitions();
        SetupReturnTransitions();
        StartInInitialState();
    }

    private void SetupSlotTransitions()
    {
        stateMachine.AddTransition<StateInSlotIdle, StateInSlotHovering>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInSlotHovering, StateInSlotIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInSlotHovering, StateInSlotPressed>((int)Event.PointerDown);
        stateMachine.AddTransition<StateInSlotPressed, StateMovingToSlot>((int)Event.PointerUp);
        stateMachine.AddTransition<StateInSlotPressed, StateDraggingFromSlot>((int)Event.BeginDrag);
        stateMachine.AddTransition<StateDraggingFromSlot, StateMovingToSlot>((int)Event.EndDrag);
    }

    private void SetupHandTransitions()
    {
        stateMachine.AddTransition<StateInHandIdle, StateInHandHover>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInHandHover, StateInHandIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInHandHover, StateInHandPressed>((int)Event.PointerDown);
        stateMachine.AddTransition<StateInHandPressed, StateMovingToSlot>((int)Event.PointerUp);
        stateMachine.AddTransition<StateInHandPressed, StateDraggingFromHand>((int)Event.BeginDrag);
        stateMachine.AddTransition<StateDraggingFromHand, StateMovingToSlot>((int)Event.EndDrag);
    }

    private void SetupReturnTransitions()
    {
        stateMachine.AddTransition<StateMovingToSlot, StateInSlotIdle>((int)Event.ReturnedToSlot);
        stateMachine.AddTransition<StateMovingToSlot, StateInHandIdle>((int)Event.ReturnedToHand);
    }

    private void StartInInitialState()
    {
        var zoneType = GetCardZoneType();

        if (zoneType == ZoneType.Hand)
            stateMachine.Start<StateInHandIdle>();
        else
            stateMachine.Start<StateInSlotIdle>();
    }

    #endregion

    #region Pointer & Drag Events

    /// <summary>
    /// Check if dragging is currently allowed.
    /// Combines: InputPolicy (phase check) AND isInteractable (valid moves check).
    /// </summary>
    private bool CanDrag
    {
        get
        {
            // Must be interactable (has valid moves, set by CardAvailabilityPresenter)
            if (!isInteractable) return false;

            // Must be in valid phase (checked by InputPolicy)
            if (inputPolicy != null && !inputPolicy.CanStartDrag) return false;

            return true;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isPointerOver = true;

        if (!PlayerCardInputState.I.IsAnotherCardBeingDragged(this))
        {
            EventBus.Publish(new CardHoverEnterEvent(this));  // Moved inside check
            stateMachine.Dispatch((int)Event.PointerEnter);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        EventBus.Publish(new CardHoverExitEvent(this));
        isPointerOver = false;
        stateMachine.Dispatch((int)Event.PointerExit);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!isInteractable) return;
        if (!CanDrag) return;
        EventBus.Publish(new CardGrabbedEvent(this));
        stateMachine.Dispatch((int)Event.PointerDown);
    }

    public void OnPointerUp(PointerEventData e)
    {
        // Always allow pointer up so FSM can leave pressed state correctly
        stateMachine.Dispatch((int)Event.PointerUp);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!isInteractable) return;
        if (!CanDrag) return;

        // Notify the policy that a drag has started
        inputPolicy?.NotifyDragStarted();

        EventBus.Publish(new CardDragBeginEvent(this));
        stateMachine.Dispatch((int)Event.BeginDrag);
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false;
    }

    public void OnDrag(PointerEventData e) { }

    public void OnEndDrag(PointerEventData e)
    {
        // Notify the policy that drag has ended
        inputPolicy?.NotifyDragEnded();

        // Always allow ending the drag if it started
        EventBus.Publish(new CardDragEndEvent(this));
        stateMachine.Dispatch((int)Event.EndDrag);
    }

    #endregion

    #region Public API - Called by States

    public RectTransform RectTransform => rectTransform;
    public RectTransform VisualTransform => visuals?.Visual;

    public void ResetToBaseScale()
    {
        animator.PlayScaleTween(initialScale);
    }

    public void ApplyHoverScale()
    {
        animator.PlayScaleTween(initialScale * config.hoverScale);
    }

    public void LiftVisual(Vector3 basePos)
    {
        animator?.LiftVisual(basePos);
    }

    public void LowerVisual(Vector3 basePos)
    {
        animator?.LowerVisual(basePos);
    }

    public void ResetVisualPosition()
    {
        if (visuals?.Visual != null)
        {
            visuals.Visual.anchoredPosition = Vector2.zero;
        }
    }

    public void SnapToMousePosition()
    {
        MoveCenterToScreenPosition(Input.mousePosition);
    }

    public void StartDraggingVisuals()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (GetCardZoneType() == ZoneType.Hand)
        {
            DOTween.Kill(rectTransform);
            rectTransform.rotation = Quaternion.identity;
        }

        animator.StartDragging();
    }

    public void EndDraggingVisuals()
    {
        RestoreInteractableState();
        ApplyZoneScale();
    }

    #endregion

    #region Public API - Animation & Movement

    public void AnimateToCurrentZone()
    {
        if (cardInstance == null || cardInstance.CurrentSlot == null)
        {
            stateMachine.Dispatch((int)Event.ReturnedToSlot);
            return;
        }

        if (GetCardZoneType() == ZoneType.Hand)
        {
            AnimateToHand();
            return;
        }

        AnimateToSlot();
    }

    private void AnimateToHand()
    {
        var layout = GetComponentInParent<HandSplineLayout>();
        if (layout)
        {
            layout.Reflow();
            DOVirtual.DelayedCall(0f, NotifyReturnedAndRehover);
        }
        else
        {
            NotifyReturnedAndRehover();
        }
    }

    private void AnimateToSlot()
    {
        var slot = cardInstance.CurrentSlot;
        var uiSlot = uiRegistry?.GetUiSlot(slot);

        if (uiSlot == null)
        {
            NotifyReturnedAndRehover();
            return;
        }

        Vector3 targetPos = CalculateSlotCenterPosition(uiSlot.RectTransform);
        animator.AnimateToPosition(targetPos, NotifyReturnedAndRehover);
    }

    public void SnapToCurrentSlot()
    {
        if (cardInstance == null || cardInstance.CurrentSlot == null)
            return;

        var zoneType = GetCardZoneType();

        if (zoneType == ZoneType.Hand)
        {
            SnapToHand();
        }
        else
        {
            SnapToBoardSlot();
        }

        ApplyZoneScale();
    }

    private void SnapToHand()
    {
        var layout = GetComponentInParent<HandSplineLayout>();
        layout?.Reflow();
    }

    private void SnapToBoardSlot()
    {
        var slot = cardInstance.CurrentSlot;
        var uiSlot = uiRegistry?.GetUiSlot(slot);

        if (uiSlot == null) return;

        rectTransform.SetParent(uiSlot.RectTransform, worldPositionStays: false);
        rectTransform.localPosition = Vector3.zero;
    }

    public void PlayAttackImpact()
    {
        float direction = (Owner == Owner.Opponent) ? -1f : 1f;
        animator.PlayAttackImpact(direction);
    }

    public void PlayHitFeedback()
    {
        visuals?.PlayHitFeedback();
    }

    #endregion

    #region Public API - Visuals & State

    public void RefreshVisual()
    {
        visuals?.RefreshFromCardInstance(cardInstance);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = interactable;

        visuals?.SetAvailabilityLight(interactable, GetCardZoneType());
    }

    public void BindRegistry(UiRegistry registry)
    {
        this.uiRegistry = registry;
    }

    /// <summary>
    /// Inject the input policy. Call this after instantiating the card.
    /// </summary>
    public void BindInputPolicy(CardInputPolicy policy)
    {
        this.inputPolicy = policy;
    }

    #endregion

    #region Helper Methods

    private void RestoreInteractableState()
    {
        // After drag, always allow hover again
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = isInteractable;
    }

    private void NotifyReturnedAndRehover()
    {
        if (this == null || stateMachine == null)
            return;

        // Dispatch return event
        if (GetCardZoneType() == ZoneType.Hand)
            stateMachine.Dispatch((int)Event.ReturnedToHand);
        else
            stateMachine.Dispatch((int)Event.ReturnedToSlot);

        // Re-trigger hover if pointer is still over
        if (isPointerOver)
            stateMachine.Dispatch((int)Event.PointerEnter);
        else
            stateMachine.Dispatch((int)Event.PointerExit);
    }

    private void ApplyZoneScale()
    {
        float targetScale = GetTargetScaleForZone();
        rectTransform.localScale = Vector3.one * targetScale;
        initialScale = rectTransform.localScale;
    }

    private float GetTargetScaleForZone()
    {
        return GetCardZoneType() == ZoneType.Hand
            ? config.handBaseScale
            : config.cellBaseScale;
    }

    private ZoneType GetCardZoneType()
    {
        if (cardInstance?.CurrentSlot?.Zone == null)
            return ZoneType.Hand;

        return cardInstance.CurrentSlot.Zone.Type;
    }

    private Camera UiCamera
    {
        get
        {
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera;
            return null;
        }
    }

    private void MoveCenterToScreenPosition(Vector2 screenPos)
    {
        Vector3 localCenterOffset = GetLocalCenterOffset();
        RectTransform parentRect = rectTransform.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect ?? rectTransform,
                screenPos,
                UiCamera,
                out var worldPoint))
        {
            Vector3 worldOffset = rectTransform.TransformVector(localCenterOffset);
            rectTransform.position = worldPoint - worldOffset;
        }
    }

    private Vector3 CalculateSlotCenterPosition(RectTransform slotRt)
    {
        Vector3 localCenterOffset = GetLocalCenterOffset();
        Vector3 slotCenterWorld = slotRt.TransformPoint(slotRt.rect.center);
        Vector3 worldOffset = rectTransform.TransformVector(localCenterOffset);

        return slotCenterWorld - worldOffset;
    }

    private Vector3 GetLocalCenterOffset()
    {
        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        return new Vector3(localCenter2D.x, localCenter2D.y, 0f);
    }

    #endregion

    #region Editor Debug

#if UNITY_EDITOR
    [Button("Debug: Play Attack Impact")]
    private void Debug_PlayAttackImpact()
    {
        if (!rectTransform)
            rectTransform = GetComponent<RectTransform>();

        Debug.Log($"[UiCard:{name}] Debug PlayAttackImpact()");
        PlayAttackImpact();
    }

    [Button("Debug: Test Lift Visual")]
    private void Debug_TestLiftVisual()
    {
        if (!visuals)
        {
            Debug.LogError($"[UiCard:{name}] visuals component is NULL!");
            return;
        }

        if (!visuals.Visual)
        {
            Debug.LogError($"[UiCard:{name}] visuals.Visual is NULL!");
            return;
        }

        Debug.Log($"[UiCard:{name}] Visual object name: {visuals.Visual.name}");
        Debug.Log($"[UiCard:{name}] Visual is child of: {visuals.Visual.parent?.name}");
        Debug.Log($"[UiCard:{name}] Card root is: {this.name}");
        Debug.Log($"[UiCard:{name}] Testing lift. Current anchored pos: {visuals.Visual.anchoredPosition}");

        var images = visuals.Visual.GetComponentsInChildren<UnityEngine.UI.Image>();
        var texts = visuals.Visual.GetComponentsInChildren<TMPro.TMP_Text>();
        Debug.Log($"[UiCard:{name}] Visual has {images.Length} Images and {texts.Length} Texts in children");

        if (animator == null && config != null)
            animator = new UiCardAnimator(rectTransform, visuals.Visual, config);

        Vector3 basePos = visuals.Visual.anchoredPosition;
        animator?.LiftVisual(basePos);

        DOVirtual.DelayedCall(1f, () =>
        {
            Debug.Log($"[UiCard:{name}] After 1 second, pos is: {visuals.Visual.anchoredPosition}");
            animator?.LowerVisual(basePos);
        });
    }
#endif

    #endregion
}