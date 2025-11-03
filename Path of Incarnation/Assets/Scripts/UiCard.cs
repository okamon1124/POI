using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using State = StateMachine<UiCard>.State;
using System.Collections.Generic;


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

    private Vector2 _initialAnchoredPos;
    private Vector3 _initialWorldPivotPos;

    private bool isPointerOver;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("CardData")]
    [SerializeField] private CardData cardData;
    public CardData CardData => cardData;


    //public Transform currentSlotTransform;

    [SerializeField] private Zone currentZone;
    public Zone CurrentZone => currentZone;

    private Zone initialZone;

    [SerializeField] private bool stateChangeDebuger = false;

    private enum Event : int
    {
        PointerEnter = 0,
        PointerExit = 1,
        PointerUp = 2,
        PointerDown = 3,
        BeginDrag = 4,
        EndDrag = 5,
        ReturnedToSlot = 6,
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (currentZone == null)
        {
            Debug.LogError("Please ensure this card has currentZone, you need to drag it in SerializeField for testing");
            return;
        }

        initialZone = currentZone;

        currentZone.TryAdd(this);

        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            Debug.LogWarning("WorldSpace Canvas has no worldCamera set. Drag position may be off.");
        }
    }

    private void Start()
    {
        initialScale = rectTransform.localScale;
        _initialAnchoredPos = rectTransform.anchoredPosition;
        _initialWorldPivotPos = rectTransform.position;

        stateMachine = new StateMachine<UiCard>(this);

        stateMachine.AddTransition<StateInSlotIdle, StateInSlotHovering>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInSlotHovering, StateInSlotIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInSlotHovering, StateInSlotPressed>((int)Event.PointerDown);
        stateMachine.AddTransition<StateInSlotPressed, StateMovingToZone>((int)Event.PointerUp);

        stateMachine.AddTransition<StateInSlotPressed, StateDraggingFromSlot>((int)Event.BeginDrag);
        stateMachine.AddTransition<StateDraggingFromSlot, StateMovingToZone>((int)Event.EndDrag);

        stateMachine.AddTransition<StateMovingToZone, StateInSlotIdle>((int)Event.ReturnedToSlot);


        stateMachine.Start<StateInSlotIdle>();

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
    }

    private void Update()
    {
        stateMachine.Update();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        //Debug.Log("Enter");
        isPointerOver = true;
        stateMachine.Dispatch((int)Event.PointerEnter);
    }
    public void OnPointerExit(PointerEventData e)
    {
        //Debug.Log("Exit");
        isPointerOver = false;
        stateMachine.Dispatch((int)Event.PointerExit);
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (!ZoneManager.I.HasValidDestination(this))
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
        //Debug.Log("BeginDrag");
        stateMachine.Dispatch((int)Event.BeginDrag);
    }
    public void OnDrag(PointerEventData e)
    {
        //Debug.Log("OnDrag");
    }
    public void OnEndDrag(PointerEventData e)
    {
        //Debug.Log("EndDrag");
        stateMachine.Dispatch((int)Event.EndDrag);
    }

    private class StateInSlotIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            //Debug.Log("Idle in Slot");
            Owner.PlayScaleTween(Owner.initialScale);
        }
    }

    private class StateInSlotHovering : State
    {
        protected override void OnEnter(State prevState)
        {
            //Debug.Log("Hovering in Slot");
            Owner.PlayScaleTween(Owner.initialScale * Owner.hoverScale);
        }
    }

    private class StateInSlotPressed : State //Pressed but not drgging
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

    private class StateDraggingFromSlot : State
    {
        protected override void OnEnter(State prevState)
        {
            // Make sure drops can be detected under this card
            if (!Owner.canvasGroup)
                Owner.canvasGroup = Owner.GetComponent<CanvasGroup>() ?? Owner.gameObject.AddComponent<CanvasGroup>();

            Owner.canvasGroup.blocksRaycasts = false;   // allow drop targets to receive pointer
            Owner.canvasGroup.interactable = false;   // optional: don't take clicks while dragging
            Owner.rectTransform.SetAsLastSibling();     // render on top while dragging

            // optional: add a subtle scale/alpha cue while dragging
            // Owner.PlayScaleTween(Owner.initialScale * Owner.hoverScale);
            // Owner.canvasGroup.alpha = 0.95f;
        }

        protected override void OnUpdate()
        {
            Owner.MoveCenterToScreen(Input.mousePosition);
        }

        protected override void OnExit(State nextState)
        {
            // Restore raycasts so the card can be clicked again
            if (Owner.canvasGroup)
            {
                Owner.canvasGroup.blocksRaycasts = true;
                Owner.canvasGroup.interactable = true;
                // Owner.canvasGroup.alpha = 1f; // if you changed it
            }
        }
    }

    private class StateMovingToZone : State
    {
        protected override void OnEnter(State prevState)
        {
            // Animate the card returning to its slot (or initial position)
            Owner.ReturnCenterToCell();
        }

        protected override void OnUpdate() { }
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

    private void ReturnCenterToCell()
    {
        // 1) Guard
        if (currentZone == null)
        {
            Debug.LogError("currentZone is null; cannot return.");
            stateMachine.Dispatch((int)Event.ReturnedToSlot);
            return;
        }

        // 2) Compute card pivot¡÷center offset (local)
        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        Vector3 localCenter = new Vector3(localCenter2D.x, localCenter2D.y, 0f);

        // 3) Zone center (world) via RectTransform
        var zoneRt = currentZone.GetComponent<RectTransform>();
        Vector3 zoneCenterWorld = zoneRt.TransformPoint(zoneRt.rect.center);

        // 4) Convert offset to world and move
        Vector3 worldOffset = rectTransform.TransformVector(localCenter);
        Vector3 targetPivotWorld = zoneCenterWorld - worldOffset;

        moveTween?.Kill();
        moveTween = rectTransform
            .DOMove(targetPivotWorld, tweenDuration)
            .SetEase(ease)
            .SetUpdate(ignoreTimeScale)
            .OnComplete(NotifyReturnedAndRehover);
    }

    // Helper used by ReturnCenterToSlot()
    private void NotifyReturnedAndRehover()
    {
        // 1) Tell the FSM we've finished returning
        stateMachine.Dispatch((int)Event.ReturnedToSlot);

        // 2) Restore correct hover state based on pointer
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

    public void AssignZone(Zone newZone)
    {
        currentZone = newZone;
    }

    public void AnimateToCurrentZone()
    {
        // Reuse your existing state path to play the tween
        Debug.Log("AnimateToCurrentZone");
        ReturnCenterToCell();
    }
}