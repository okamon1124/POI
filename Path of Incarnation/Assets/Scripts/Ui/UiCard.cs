using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using State = StateMachine<UiCard>.State;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class UiCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private StateMachine<UiCard> stateMachine;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector3 initialScale;
    private Tween scaleTween;
    private Tween moveTween;

    private bool isPointerOver;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("CardData")]
    [SerializeField] private CardData cardData;
    public CardData CardData => cardData;

    public Team OwnerTeam = Team.Both;

    [SerializeField] private UiZone currentZone;
    public UiZone CurrentZone => currentZone;

    [Header("Visuals")]
    [SerializeField] private RectTransform visual;

    [Header("Zone Scales")]
    [SerializeField] private float handBaseScale = 1.4f;     // bigger when in hand
    [SerializeField] private float cellBaseScale = 1.1f;     // normal board/cell scale
    [SerializeField] private float draggingScale = 1.1f;     // scale while dragging

    [SerializeField] private float liftAmount = 30f;

    [SerializeField] private bool stateChangeDebuger = false;

    [Header("Availability Light")]
    [SerializeField] private Light2D availabilityLight;
    [SerializeField] private float availableLightIntensity = 1.5f;
    [SerializeField] private float unavailableLightIntensity = 0f;

    private bool lastHasValidDestination;

    private enum Event : int
    {
        PointerEnter = 0,
        PointerExit = 1,
        PointerUp = 2,
        PointerDown = 3,
        BeginDrag = 4,
        EndDrag = 5,
        ReturnedToCell = 6,
        ReturnedToHand = 7,
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BoardStateChangedEvent>(OnBoardStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BoardStateChangedEvent>(OnBoardStateChanged);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (currentZone != null)
        {
            currentZone.TryAdd(this);
        }

        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            Debug.LogWarning("WorldSpace Canvas has no worldCamera set. Drag position may be off.");
        }
    }

    private void Start()
    {
        initialScale = rectTransform.localScale;

        stateMachine = new StateMachine<UiCard>(this);

        // ---- CELL branch ----
        stateMachine.AddTransition<StateInCellIdle, StateInCellHovering>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInCellHovering, StateInCellIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInCellHovering, StateInCellPressed>((int)Event.PointerDown);
        stateMachine.AddTransition<StateInCellPressed, StateMovingToZone>((int)Event.PointerUp);
        stateMachine.AddTransition<StateInCellPressed, StateDraggingFromCell>((int)Event.BeginDrag);
        stateMachine.AddTransition<StateDraggingFromCell, StateMovingToZone>((int)Event.EndDrag);

        // Return target depends on where the card currently lives
        stateMachine.AddTransition<StateMovingToZone, StateInCellIdle>((int)Event.ReturnedToCell);
        stateMachine.AddTransition<StateMovingToZone, StateInHandIdle>((int)Event.ReturnedToHand);

        // ---- HAND branch ----
        stateMachine.AddTransition<StateInHandIdle, StateInHandHover>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInHandHover, StateInHandIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInHandHover, StateInCellPressed>((int)Event.PointerDown);


        if (currentZone.zoneType == ZoneType.Hand)
            stateMachine.Start<StateInHandIdle>();
        else
            stateMachine.Start<StateInCellIdle>();

        stateMachine.OnStateChanged += (from, to, evt) =>
        {
            var fromName = from?.GetType().Name ?? "null";
            var toName = to?.GetType().Name ?? "null";
            string evtName = System.Enum.IsDefined(typeof(Event), evt) ? ((Event)evt).ToString() : evt.ToString();

            if (stateChangeDebuger)
            {
                Debug.Log($"<color=white>[UiCard:{name}]</color> <color=yellow>{fromName}</color> " +
                $"--(<color=orange>{evtName}</color>)--> <color=yellow>{toName}</color>");
            }
        };

        ApplyZoneScale();
        RefreshAvailabilityLight(force: true);
    }

    private void Update()
    {
        stateMachine.Update();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        EventBus.Publish(new CardHoverEnterEvent(this));
        isPointerOver = true;

        if (PlayerControlState.I.IsAnotherCardBeingDragged(this))
        {
            Debug.Log("Don't scale up because another card is dragging");
        }
        else
        {
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
        //var zm = ZoneManager.I;
        //var cz = CurrentZone;
        //var czName = cz ? cz.zoneType.ToString() : "NULL";
        //
        //List<Zone> valids = zm ? zm.GetValidDestinations(this) : null;
        //int zonesCount = zm?.Zones?.Count ?? -1;
        //int validCount = valids?.Count ?? -1;
        //
        //Debug.Log($"[Probe] {name} click | CurrentZone={czName} | Zones={zonesCount} | Valid={validCount}");
        //
        //if (valids != null)
        //    foreach (var z in valids)
        //        Debug.Log($"[Probe] -> {z.zoneType} (full={z.IsFull})");

        if (!HasValidDestination)
        {
            Debug.Log($"{name}: click disabled (no valid move).");
            return;
        }

        //Debug.Log("Down");
        stateMachine.Dispatch((int)Event.PointerDown);
    }
    public void OnPointerUp(PointerEventData e)
    {
        //Debug.Log("Up");
        stateMachine.Dispatch((int)Event.PointerUp);
    }
    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        stateMachine.Dispatch((int)Event.BeginDrag);
    }
    public void OnDrag(PointerEventData e)
    {
        //Debug.Log("OnDrag");
    }
    public void OnEndDrag(PointerEventData e)
    {
        EventBus.Publish(new CardDragEndEvent(this));
        stateMachine.Dispatch((int)Event.EndDrag);
    }

    private class StateInCellIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            //Debug.Log("Idle in Slot");
            Owner.PlayScaleTween(Owner.initialScale);
        }
    }

    private class StateInCellHovering : State
    {
        protected override void OnEnter(State prevState)
        {
            //Debug.Log("Hovering in Slot");
            Owner.PlayScaleTween(Owner.initialScale * Owner.hoverScale);
        }
    }

    private class StateInCellPressed : State //Pressed but not drgging
    {
        protected override void OnEnter(State prevState)
        {
            //Debug.Log("Pressing..");
            Owner.MoveCenterToScreen(Input.mousePosition);
        }

        protected override void OnExit(State nextState)
        {
            //Debug.Log("Stop Pressing");
        }
    }

    private class StateDraggingFromCell : State
    {
        private Vector3 _savedScale;

        protected override void OnEnter(State prevState)
        {
            if (!Owner.canvasGroup)
                Owner.canvasGroup = Owner.GetComponent<CanvasGroup>() ?? Owner.gameObject.AddComponent<CanvasGroup>();

            Owner.canvasGroup.blocksRaycasts = false;
            Owner.canvasGroup.interactable = false;
            Owner.rectTransform.SetAsLastSibling();

            // flatten rotation if dragging from hand
            if (Owner.currentZone && Owner.currentZone.zoneType == ZoneType.Hand)
            {
                DOTween.Kill(Owner.rectTransform);
                Owner.rectTransform.rotation = Quaternion.identity;
            }

            EventBus.Publish(new CardDragBeginEvent(Owner));

            // --- Apply drag scale ---
            _savedScale = Owner.rectTransform.localScale;
            Owner.rectTransform.DOScale(Vector3.one * Owner.draggingScale, Owner.tweenDuration * 0.5f)
                .SetEase(Owner.ease)
                .SetUpdate(Owner.ignoreTimeScale);
        }

        protected override void OnUpdate()
        {
            Owner.MoveCenterToScreen(Input.mousePosition);
        }

        protected override void OnExit(State nextState)
        {
            // restore raycast
            if (Owner.canvasGroup)
            {
                Owner.canvasGroup.blocksRaycasts = true;
                Owner.canvasGroup.interactable = true;
            }

            // --- Restore correct base scale for the destination zone ---
            Owner.ApplyZoneScale();
        }
    }

    private class StateMovingToZone : State
    {
        protected override void OnEnter(State prevState)
        {
            // Animate the card returning to its slot (or initial position)
            Owner.AnimateToCurrentZone();
        }

        protected override void OnUpdate() { }
    }

    private class StateInHandIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.PlayScaleTween(Owner.initialScale);
        }
    }

    private class StateInHandHover : State
    {
        private int _savedSibling;
        private Vector3 _baseLocalPos;     // the original resting position in layout
        private bool _cachedBasePos = false;

        protected override void OnEnter(State prevState)
        {
            Owner.PlayScaleTween(Owner.initialScale * Owner.hoverScale);

            if (Owner.rectTransform)
            {
                _savedSibling = Owner.rectTransform.GetSiblingIndex();
                Owner.rectTransform.SetAsLastSibling();
            }

            if (Owner.visual)
            {
                if (!_cachedBasePos)
                {
                    // remember the starting position only once
                    _baseLocalPos = Owner.visual.localPosition;
                    _cachedBasePos = true;
                }

                // move relative to base each time
                Vector3 lifted = _baseLocalPos + new Vector3(0f, Owner.liftAmount, 0f);
                Owner.visual.DOKill(false);
                Owner.visual.DOLocalMove(lifted, Owner.tweenDuration)
                    .SetEase(Owner.ease)
                    .SetUpdate(Owner.ignoreTimeScale)
                    .SetId("hover_vis_move");
            }
        }

        protected override void OnExit(State nextState)
        {
            if (Owner.visual)
            {
                DOTween.Kill("hover_vis_move");

                // Force reset position (guard against interrupted tween)
                Owner.visual.localPosition = _baseLocalPos;

                // Optional: smooth return if you want a visible motion
                Owner.visual.DOLocalMove(_baseLocalPos, Owner.tweenDuration)
                    .SetEase(Owner.ease)
                    .SetUpdate(Owner.ignoreTimeScale)
                    .SetId("hover_vis_move");
            }

            if (!(nextState is StateDraggingFromCell) && Owner.rectTransform)
                Owner.rectTransform.SetSiblingIndex(_savedSibling);

            Owner.PlayScaleTween(Owner.initialScale);
        }
    }


    private void PlayScaleTween(Vector3 target)
    {
        // Kill any existing tween to avoid overlap
        scaleTween?.Kill();

        // Use SetUpdate(true) to run independent of Time.timeScale when desired
        scaleTween = rectTransform
            .DOScale(target, tweenDuration)
            .SetEase(ease)
            .SetUpdate(ignoreTimeScale);
    }

    private Camera UiCamera
    {
        get
        {
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera;
            else
                return null;
        }
    }

    public void AnimateToCurrentZone()
    {
        if (currentZone == null)
        {
            Debug.LogError("currentZone is null; cannot return.");
            stateMachine.Dispatch((int)Event.ReturnedToCell);
            return;
        }

        if (currentZone.zoneType == ZoneType.Hand)
        {
            var layout = currentZone.GetComponent<HandSplineLayout>();
            if (layout)
            {
                layout.Reflow();

                // Defer the state machine event so it's not fired inside OnEnter.
                DOVirtual.DelayedCall(0f, NotifyReturnedAndRehover)
                    .SetUpdate(ignoreTimeScale); // keep consistent with your tweens
                return;
            }
        }

        // Default behavior (non-hand zones): tween pivot to zone center
        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        Vector3 localCenter = new Vector3(localCenter2D.x, localCenter2D.y, 0f);

        var zoneRt = currentZone.GetComponent<RectTransform>();
        Vector3 zoneCenterWorld = zoneRt.TransformPoint(zoneRt.rect.center);

        Vector3 worldOffset = rectTransform.TransformVector(localCenter);
        Vector3 targetPivotWorld = zoneCenterWorld - worldOffset;

        moveTween?.Kill();
        moveTween = rectTransform
            .DOMove(targetPivotWorld, tweenDuration)
            .SetEase(ease)
            .SetUpdate(ignoreTimeScale)
            .OnComplete(NotifyReturnedAndRehover);
    }

    private void NotifyReturnedAndRehover()
    {
        // choose destination idle based on current zone
        if (currentZone != null && currentZone.zoneType == ZoneType.Hand)
            stateMachine.Dispatch((int)Event.ReturnedToHand);
        else
            stateMachine.Dispatch((int)Event.ReturnedToCell);

        // re-apply hover if pointer is still over
        if (isPointerOver)
            stateMachine.Dispatch((int)Event.PointerEnter);
        else
            stateMachine.Dispatch((int)Event.PointerExit);
    }

    private Vector3 MoveCenterToScreen(Vector2 screenPos)
    {
        // pivot ¡÷ center offset
        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        Vector3 localCenter = new Vector3(localCenter2D.x, localCenter2D.y, 0f);

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect ? parentRect : rectTransform, screenPos, UiCamera, out var worldPoint))
        {
            Vector3 worldOffset = rectTransform.TransformVector(localCenter);
            rectTransform.position = worldPoint - worldOffset;
        }

        return rectTransform.position;
    }

    /// <summary>
    /// Only ZoneManager should call this when a move actually succeeds.
    /// </summary>
    public void AssignZone(UiZone z)
    {
        currentZone = z;
        ApplyZoneScale();
    }

    private void ApplyZoneScale()
    {
        if (!rectTransform) return;

        float target =
            currentZone && currentZone.zoneType == ZoneType.Hand
            ? handBaseScale
            : cellBaseScale;

        rectTransform.localScale = Vector3.one * target;
        initialScale = rectTransform.localScale;   // update for hover baseline
    }

    private bool HasValidDestination
    {
        get
        {
            return ZoneManager.I != null && ZoneManager.I.HasValidDestination(this);
        }
    }

    private void RefreshAvailabilityLight(bool force = false)
    {
        if (!availabilityLight || ZoneManager.I == null)
            return;

        bool hasValidDestination = ZoneManager.I.HasValidDestination(this);

        Debug.Log($"hasValidDestination:{hasValidDestination}");

        if (!force && hasValidDestination == lastHasValidDestination)
            return; // no change, no work

        lastHasValidDestination = hasValidDestination;

        availabilityLight.intensity = hasValidDestination ? availableLightIntensity : unavailableLightIntensity;
    }

    private void OnBoardStateChanged(BoardStateChangedEvent ev)
    {
        
        RefreshAvailabilityLight(force: true);
    }
}