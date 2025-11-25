using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using State = StateMachine<UiCard>.State;

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

    [Header("Logic Link")]
    public CardInstance cardInstance;
    public Owner Owner => cardInstance != null ? cardInstance.Owner : Owner.None;

    [Header("Board Reference")]
    [SerializeField] private UiRegistry uiRegistry;

    public void BindRegistry(UiRegistry registry)
    {
        this.uiRegistry = registry;
    }

    [Header("Visuals")]
    [SerializeField] private RectTransform visual;

    [Header("Visual Refs")]
    [SerializeField] private Image artImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text speedText;

    [Header("Zone Scales")]
    [SerializeField] private float handBaseScale = 1.4f;
    [SerializeField] private float cellBaseScale = 1.1f;
    [SerializeField] private float draggingScale = 1.1f;

    [SerializeField] private float liftAmount = 30f;
    [SerializeField] private bool stateChangeDebuger = false;

    [Header("Availability Light")]
    [SerializeField] private Light2D availabilityLight;
    [SerializeField] private float availableLightIntensity = 1.5f;
    [SerializeField] private float unavailableLightIntensity = 0f;

    [Header("Owner Light")]
    [SerializeField] private Light2D enemyLight;

    private bool _interactable = true;

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
        NotInt
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            Debug.LogWarning("WorldSpace Canvas has no worldCamera set. Drag position may be off.");
        }
    }

    private void Start()
    {
        initialScale = rectTransform.localScale;

        stateMachine = new StateMachine<UiCard>(this);

        // CELL branch
        stateMachine.AddTransition<StateInCellIdle, StateInCellHovering>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInCellHovering, StateInCellIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInCellHovering, StateInCellPressed>((int)Event.PointerDown);
        stateMachine.AddTransition<StateInCellPressed, StateMovingToZone>((int)Event.PointerUp);
        stateMachine.AddTransition<StateInCellPressed, StateDraggingFromCell>((int)Event.BeginDrag);
        stateMachine.AddTransition<StateDraggingFromCell, StateMovingToZone>((int)Event.EndDrag);

        // return target
        stateMachine.AddTransition<StateMovingToZone, StateInCellIdle>((int)Event.ReturnedToCell);
        stateMachine.AddTransition<StateMovingToZone, StateInHandIdle>((int)Event.ReturnedToHand);

        // HAND branch
        stateMachine.AddTransition<StateInHandIdle, StateInHandHover>((int)Event.PointerEnter);
        stateMachine.AddTransition<StateInHandHover, StateInHandIdle>((int)Event.PointerExit);
        stateMachine.AddTransition<StateInHandHover, StateInCellPressed>((int)Event.PointerDown);

        var zoneType = GetCardZoneType();
        if (zoneType == ZoneType.Hand)
            stateMachine.Start<StateInHandIdle>();
        else
            stateMachine.Start<StateInCellIdle>();

        stateMachine.OnStateChanged += (from, to, evt) =>
        {
            if (!stateChangeDebuger) return;

            var fromName = from?.GetType().Name ?? "null";
            var toName = to?.GetType().Name ?? "null";
            string evtName = System.Enum.IsDefined(typeof(Event), evt) ? ((Event)evt).ToString() : evt.ToString();

            Debug.Log($"<color=white>[UiCard:{name}]</color> <color=yellow>{fromName}</color> " +
                      $"--(<color=orange>{evtName}</color>)--> <color=yellow>{toName}</color>");
        };
    }

    private void Update()
    {
        stateMachine.Update();
    }

    #region Pointer & Drag

    public void OnPointerEnter(PointerEventData e)
    {
        EventBus.Publish(new CardHoverEnterEvent(this));
        isPointerOver = true;

        if (PlayerCardInputState.I.IsAnotherCardBeingDragged(this))
        {
            // no hover if another card dragging
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
        stateMachine.Dispatch((int)Event.PointerDown);
    }

    public void OnPointerUp(PointerEventData e)
    {
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

    public void OnDrag(PointerEventData e) { }

    public void OnEndDrag(PointerEventData e)
    {
        EventBus.Publish(new CardDragEndEvent(this));
        stateMachine.Dispatch((int)Event.EndDrag);
    }

    #endregion

    #region States

    private class StateInCellIdle : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.PlayScaleTween(Owner.initialScale);
        }
    }

    private class StateInCellHovering : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.PlayScaleTween(Owner.initialScale * Owner.hoverScale);
        }
    }

    private class StateInCellPressed : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.MoveCenterToScreen(Input.mousePosition);
        }
    }

    private class StateDraggingFromCell : State
    {
        protected override void OnEnter(State prevState)
        {
            if (!Owner.canvasGroup)
                Owner.canvasGroup = Owner.GetComponent<CanvasGroup>() ?? Owner.gameObject.AddComponent<CanvasGroup>();

            Owner.canvasGroup.blocksRaycasts = false;
            Owner.canvasGroup.interactable = false;
            Owner.rectTransform.SetAsLastSibling();

            if (Owner.GetCardZoneType() == ZoneType.Hand)
            {
                DOTween.Kill(Owner.rectTransform);
                Owner.rectTransform.rotation = Quaternion.identity;
            }

            EventBus.Publish(new CardDragBeginEvent(Owner));

            Owner.rectTransform.DOScale(Vector3.one * Owner.draggingScale, Owner.tweenDuration * 0.5f)
                .SetEase(Owner.ease);
        }

        protected override void OnUpdate()
        {
            Owner.MoveCenterToScreen(Input.mousePosition);
        }

        protected override void OnExit(State nextState)
        {
            // 這裡不要自己猜 true / false，
            // 直接用 UiCard 裡目前記錄的 _interactable 來還原
            Owner.SetInteractable(Owner._interactable);

            Owner.ApplyZoneScale();
        }
    }

    private class StateMovingToZone : State
    {
        protected override void OnEnter(State prevState)
        {
            Owner.AnimateToCurrentZone();
        }
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
        private Vector3 _baseLocalPos;
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
                    _baseLocalPos = Owner.visual.localPosition;
                    _cachedBasePos = true;
                }

                Vector3 lifted = _baseLocalPos + new Vector3(0f, Owner.liftAmount, 0f);
                Owner.visual.DOKill(false);
                Owner.visual.DOLocalMove(lifted, Owner.tweenDuration)
                    .SetEase(Owner.ease)
                    .SetId("hover_vis_move");
            }
        }

        protected override void OnExit(State nextState)
        {
            if (Owner.visual)
            {
                DOTween.Kill("hover_vis_move");
                Owner.visual.localPosition = _baseLocalPos;

                Owner.visual.DOLocalMove(_baseLocalPos, Owner.tweenDuration)
                    .SetEase(Owner.ease)
                    .SetId("hover_vis_move");
            }

            if (!(nextState is StateDraggingFromCell) && Owner.rectTransform)
                Owner.rectTransform.SetSiblingIndex(_savedSibling);

            Owner.PlayScaleTween(Owner.initialScale);
        }
    }

    #endregion

    #region Visual Helpers

    private void PlayScaleTween(Vector3 target)
    {
        scaleTween?.Kill();

        scaleTween = rectTransform
            .DOScale(target, tweenDuration)
            .SetEase(ease);
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

    private Vector3 MoveCenterToScreen(Vector2 screenPos)
    {
        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        Vector3 localCenter = new Vector3(localCenter2D.x, localCenter2D.y, 0f);

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect ? parentRect : rectTransform,
                screenPos,
                UiCamera,
                out var worldPoint))
        {
            Vector3 worldOffset = rectTransform.TransformVector(localCenter);
            rectTransform.position = worldPoint - worldOffset;
        }

        return rectTransform.position;
    }

    public void AnimateToCurrentZone()
    {
        if (cardInstance == null || cardInstance.CurrentSlot == null)
        {
            stateMachine.Dispatch((int)Event.ReturnedToCell);
            return;
        }

        if (GetCardZoneType() == ZoneType.Hand)
        {
            var layout = GetComponentInParent<HandSplineLayout>();
            if (layout)
            {
                layout.Reflow();

                DOVirtual.DelayedCall(0f, NotifyReturnedAndRehover);
                return;
            }
        }

        var slot = cardInstance.CurrentSlot;
        var uiSlot = uiRegistry != null ? uiRegistry.GetUiSlot(slot) : null;

        if (uiSlot == null)
        {
            NotifyReturnedAndRehover();
            return;
        }

        RectTransform slotRt = uiSlot.RectTransform;

        Vector2 size = rectTransform.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rectTransform.pivot) * size;
        Vector3 localCenter = new Vector3(localCenter2D.x, localCenter2D.y, 0f);

        Vector3 slotCenterWorld = slotRt.TransformPoint(slotRt.rect.center);
        Vector3 worldOffset = rectTransform.TransformVector(localCenter);
        Vector3 targetPivotWorld = slotCenterWorld - worldOffset;

        moveTween?.Kill();
        moveTween = rectTransform
            .DOMove(targetPivotWorld, tweenDuration)
            .SetEase(ease)
            .OnComplete(NotifyReturnedAndRehover);
    }

    public void SnapToCurrentSlot()
    {
        if (cardInstance == null || cardInstance.CurrentSlot == null)
            return;

        var zoneType = GetCardZoneType();

        // --- HAND: no UiSlots, use spline layout ---
        if (zoneType == ZoneType.Hand)
        {
            // we assume the card is already parented under CardsInHandRoot
            // (HandDrawPresenter instantiates it there)
            var layout = GetComponentInParent<HandSplineLayout>();
            if (layout != null)
            {
                layout.Reflow();   // position all hand cards along the spline
            }

            ApplyZoneScale();
            return;
        }

        // --- BOARD ZONES: use UiSlot parenting (old behavior) ---
        var slot = cardInstance.CurrentSlot;
        var uiSlot = uiRegistry != null ? uiRegistry.GetUiSlot(slot) : null;
        if (uiSlot == null) return;

        rectTransform.SetParent(uiSlot.RectTransform, worldPositionStays: false);
        rectTransform.localPosition = Vector3.zero;
        ApplyZoneScale();
    }


    private void NotifyReturnedAndRehover()
    {
        if (GetCardZoneType() == ZoneType.Hand)
            stateMachine.Dispatch((int)Event.ReturnedToHand);
        else
            stateMachine.Dispatch((int)Event.ReturnedToCell);

        if (isPointerOver)
            stateMachine.Dispatch((int)Event.PointerEnter);
        else
            stateMachine.Dispatch((int)Event.PointerExit);
    }

    private void ApplyZoneScale()
    {
        if (!rectTransform) return;

        float target = GetCardZoneType() == ZoneType.Hand
            ? handBaseScale
            : cellBaseScale;

        rectTransform.localScale = Vector3.one * target;
        initialScale = rectTransform.localScale;
    }

    private ZoneType GetCardZoneType()
    {
        if (cardInstance == null || cardInstance.CurrentSlot == null || cardInstance.CurrentSlot.Zone == null)
            return ZoneType.Hand;

        return cardInstance.CurrentSlot.Zone.Type;
    }

    public void RefreshVisual()
    {
        if (cardInstance == null) return;

        var data = cardInstance.Data;

        if (artImage) artImage.sprite = data.cardSprite;
        if (nameText) nameText.text = data.cardName;
        if (costText) costText.text = data.manaCost.ToString();

        if (powerText) powerText.text = cardInstance.CurrentPower.ToString();
        if (healthText) healthText.text = cardInstance.CurrentHealth.ToString();
        if (speedText) speedText.text = cardInstance.CurrentSpeed.ToString();

        if (enemyLight != null)
        {
            enemyLight.enabled = (Owner == Owner.Opponent);
        }
    }

    public void PlayAttackImpact()
    {
        if (rectTransform == null)
        {
            Debug.LogError($"[UiCard:{name}] rectTransform is null, cannot play attack impact.");
            return;
        }

        // dir：畜力方向，攻擊方向會是 -dir
        float dir = (Owner == Owner.Opponent) ? -1f : 1f;

        // 距離
        float windupDist = 0.2f;  // 畜力：小退
        float attackDist = 0.5f;  // 攻擊：大撞（你說希望更長，我直接開大一點，自己再微調）

        // 節奏
        float windupTime = 0.08f;
        float attackTime = 0.12f;
        float recoverTime = 0.15f;

        // 旋轉量
        float windupAngle = 6f;    // 畜力：頭往反方向小傾斜
        float attackAngle = 18f;   // 攻擊：頭往攻擊方向大傾斜

        Vector3 startPos = rectTransform.position;
        Vector3 startEuler = rectTransform.eulerAngles;

        rectTransform.DOKill();

        // 位置：先往 dir 畜力，再往 -dir 攻擊
        Vector3 windupPos = startPos + new Vector3(dir * windupDist, 0f, 0f);    // 反方向
        Vector3 attackPos = startPos + new Vector3(-dir * attackDist, 0f, 0f);   // 攻擊方向

        Sequence seq = DOTween.Sequence();

        // 1) 畜力：往反方向小退 + 頭往反方向（-dir）傾一點
        seq.Append(
            rectTransform.DOMove(windupPos, windupTime)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            rectTransform.DORotate(
                    startEuler + new Vector3(0f, 0f, -dir * windupAngle),
                    windupTime
                )
                .SetEase(Ease.OutQuad)
        );

        // 2) 攻擊：往 -dir 大撞 + 頭往攻擊方向（= -dir）大傾斜
        //    但公式推完會是「dir * attackAngle」，這樣頭會對準攻擊方向
        seq.Append(
            rectTransform.DOMove(attackPos, attackTime)
                .SetEase(Ease.InQuad)
        );
        seq.Join(
            rectTransform.DORotate(
                    startEuler + new Vector3(0f, 0f, dir * attackAngle),
                    attackTime * 0.7f
                )
                .SetEase(Ease.OutQuad)
        );

        // 3) 收回：回到原位置 + 原角度
        seq.Append(
            rectTransform.DOMove(startPos, recoverTime)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            rectTransform.DORotate(startEuler, recoverTime)
                .SetEase(Ease.OutQuad)
        );
    }

    #endregion

    #region Public API for Logic

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;   // ← 記起來

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.interactable = interactable;

        RefreshAvailabilityLight(interactable);
    }


    private void RefreshAvailabilityLight(bool interactable)
    {
        if (!availabilityLight) return;

        availabilityLight.intensity = interactable
            ? availableLightIntensity
            : unavailableLightIntensity;
    }

    #endregion

#if UNITY_EDITOR
    [Button("Debug: Play Attack Impact")]
    private void Debug_PlayAttackImpact()
    {
        if (!rectTransform)
            rectTransform = GetComponent<RectTransform>();

        Debug.Log($"[UiCard:{name}] Debug PlayAttackImpact()");
        PlayAttackImpact();
    }
#endif
}