using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class UIDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Grid (optional, found at runtime)")]
    private CardGrid cardGrid;

    [Header("Snap")]
    [Tooltip("If true, reparent to Field when snapped to a slot.")]
    [SerializeField] private bool reparentOnSnap = true;

    [Header("Field Parent")]
    [Tooltip("Set this to your 'Field' transform in the scene.")]
    private Transform fieldParent;

    [Header("Hand (optional)"), HideInInspector]
    public HandUiManager handManager;

    [Header("Drag / Slot Visuals")]
    [SerializeField] private float slotScale = 1.1f;
    [SerializeField] private float dragScaleDuration = 0.08f;

    [Header("Miss Return (slot→slot)")]
    [SerializeField] private float missReturnDuration = 0.18f;
    [SerializeField] private Ease missReturnEase = Ease.OutQuad;

    private enum DragState { Idle, PointerDown, Dragging, ReturningToHand, ReturningToSlot, Snapped }
    private DragState state = DragState.Idle;

    private RectTransform rt;
    private Vector3 originalScale;
    private bool pointerActive;
    private bool cameFromHand;

    private RectTransform lastSlotParent;
    private Vector2 lastSlotLocal;
    private bool isInSlot;

    public Action dragStarted;
    public Action dragEnded;

    private bool isPointerOver;

    private bool countsForHandHover;       // this rect is contributing to hand hover

    private bool IsInHand =>
        handManager && handManager.Contains(rt) && rt.parent == handManager.transform;

    // tween ids
    private string POS_ID => $"drag_pos_{rt.GetInstanceID()}";
    private string SCALE_ID => $"drag_scale_{rt.GetInstanceID()}";
    private string HAND_LAYOUT_ID => $"hand_layout_{rt.GetInstanceID()}";
    private string GRID_POS_ID => $"grid_pos_{rt.GetInstanceID()}"; // matches CardGrid

    private bool HandBlocked => handManager && handManager.IsBusy;

    private ReferenceContext context;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        originalScale = rt.localScale;
        ResolveRuntimeRefs();
    }

    private void Update()
    {
        // watchdog: finalize ReturnToHand if hand layout tween vanished
        if (state == DragState.ReturningToHand && !DOTween.IsTweening(HAND_LAYOUT_ID))
        {
            isInSlot = false;
            FinishDragVisuals(stayScaled: false);
            state = DragState.Idle;
        }
    }

    private void OnTransformParentChanged() => ResolveRuntimeRefs();

    private void ResolveRuntimeRefs()
    {
        if (!cardGrid && transform.parent)
            cardGrid = transform.parent.GetComponentInChildren<CardGrid>();
        if (!cardGrid) cardGrid = FindFirstObjectByType<CardGrid>();

        // Runtime lookup only (no serialization)
        if (!context)
            context = GetComponentInParent<ReferenceContext>();

        if (context)
        {
            if (!fieldParent) fieldParent = context.FieldParent;
            if (!cardGrid) cardGrid = context.Grid;
        }

        if (!cardGrid)
            Debug.LogWarning($"{name}: No CardGrid found (parent/sibling/scene/context).");
        if (!fieldParent && reparentOnSnap)
            Debug.LogWarning($"{name}: Reparent On Snap is ON but Field Parent is NULL (context missing?).");
    }

    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

    public void OnPointerEnter(PointerEventData e)
    {
        if (HandBlocked) return;
        if (isPointerOver) return;
        isPointerOver = true;

        // Only lift the hand if this card is still in the hand
        if (handManager && IsInHand && !countsForHandHover)
        {
            countsForHandHover = true;
            handManager.CardHoverEnter(rt);   // uses the coalescing logic
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!isPointerOver) return;
        isPointerOver = false;

        if (countsForHandHover && handManager)
        {
            countsForHandHover = false;
            handManager.CardHoverExit(rt);
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (HandBlocked) return;

        if (state == DragState.ReturningToHand || state == DragState.ReturningToSlot) return;
        pointerActive = true;

        dragStarted?.Invoke();

        DOTween.Kill(HAND_LAYOUT_ID);
        rt.SetAsLastSibling();

        cameFromHand = handManager && handManager.Contains(rt);
        if (cameFromHand) handManager.SetDragging(rt, true);

        rt.localRotation = Quaternion.identity;
        SetScaleAbs(slotScale, dragScaleDuration);

        cardGrid?.SetDrag(rt, true);
        MoveToPointerCenter(e);

        state = DragState.PointerDown;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (HandBlocked || !CanDrag()) return;
        if (!CanDrag()) return;
        state = DragState.Dragging;
        MoveToPointerCenter(e);

        if (handManager && cameFromHand)
            handManager.SetHandHover(false);
    }

    public void OnDrag(PointerEventData e)
    {
        if (HandBlocked || !CanDrag()) return;
        if (!CanDrag()) return;
        MoveToPointerCenter(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (HandBlocked || !CanDrag()) return;
        if (!CanDrag()) return;

        // ✂️ Removed the forced SetHandHover(true) here.

        CompleteInteraction();

        if (handManager) StartCoroutine(DelayedHoverRefresh());
    }

    private IEnumerator DelayedHoverRefresh()
    {
        yield return null; // let detach/reparent finish
        handManager?.RefreshHoverFromPointer(rt); // ignore this card while deciding hover
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!pointerActive) return;
        if (HandBlocked) { pointerActive = false; return; }
        pointerActive = false;

        dragEnded?.Invoke();

        if (state == DragState.ReturningToHand || state == DragState.ReturningToSlot) return;
        CompleteInteraction();

        if (handManager) handManager.RefreshHoverFromPointer();
    }

    private bool CanDrag() =>
        state != DragState.ReturningToHand && state != DragState.ReturningToSlot;

    private void CompleteInteraction()
    {
        cardGrid?.SetDrag(null, false);

        if (cardGrid != null &&
            cardGrid.TrySnapFromDraggable(rt, /* reparentOnSnap from grid not needed */ false,
                out var snappedParent, out var snappedLocal))
        {
            OnSnappedToSlot(snappedParent, snappedLocal);
            return;
        }

        OnDropMissed();
    }

    private void OnSnappedToSlot(RectTransform snappedParent, Vector2 snappedLocal)
    {
        if (cameFromHand && handManager) handManager.DetachCard(rt);

        // If the pointer is still over this card as it moves to Field,
        // ensure it no longer counts toward hand hover.
        if (countsForHandHover && handManager && !IsInHand)
        {
            countsForHandHover = false;
            handManager.CardHoverExit(rt);
        }

        // kill any grid snap tween that might have been started
        DOTween.Kill(GRID_POS_ID);

        // convert snapped point (in snappedParent local) -> world -> field local
        Vector3 targetWorld = snappedParent.TransformPoint(new Vector3(snappedLocal.x, snappedLocal.y, 0f));

        if (reparentOnSnap && fieldParent != null)
        {
            var fieldRT = (RectTransform)fieldParent;

            // reparent to field, preserving current world pos first
            rt.SetParent(fieldParent, worldPositionStays: true);

            // compute local point in field space and animate there
            Vector3 localInField = fieldRT.InverseTransformPoint(targetWorld);

            SetScaleAbs(slotScale, 0f); // keep ABSOLUTE slot scale on field
            DOTween.Kill(POS_ID);
            rt.DOAnchorPos3D(localInField, missReturnDuration)
              .SetEase(missReturnEase)
              .SetId(POS_ID);

            lastSlotParent = fieldRT;
            lastSlotLocal = localInField;
        }
        else
        {
            // default: stay under the grid's parent
            if (rt.parent != snappedParent)
                rt.SetParent(snappedParent, worldPositionStays: true);

            SetScaleAbs(slotScale, 0f);
            DOTween.Kill(POS_ID);
            rt.DOAnchorPos(snappedLocal, missReturnDuration)
              .SetEase(missReturnEase)
              .SetId(POS_ID);

            lastSlotParent = snappedParent;
            lastSlotLocal = snappedLocal;
        }

        isInSlot = true;
        FinishDragVisuals(stayScaled: true);
        state = DragState.Snapped;
    }

    private void OnDropMissed()
    {
        bool shouldReturnToHand = handManager && (cameFromHand || handManager.Contains(rt));

        if (shouldReturnToHand) ReturnToHand();
        else if (isInSlot && lastSlotParent) ReturnToLastSlot();
        else FinishDragVisuals(stayScaled: false);
    }

    private void ReturnToHand()
    {
        state = DragState.ReturningToHand;

        DOTween.Kill(POS_ID);
        SetScaleOriginal(dragScaleDuration);
        if (handManager) handManager.SetDragging(rt, false);

        // This card must not count toward hover while it flies back
        if (countsForHandHover && handManager)
        {
            countsForHandHover = false;
            handManager.CardHoverExit(rt);
        }
        handManager?.RefreshHoverFromPointer(rt); // recompute while ignoring this card

        handManager.ReturnCardToHand(rt, onLaidOut: () =>
        {
            isInSlot = false;
            FinishDragVisuals(stayScaled: false);
            state = DragState.Idle;
        });
    }

    private void ReturnToLastSlot()
    {
        state = DragState.ReturningToSlot;

        DOTween.Kill(POS_ID);

        if (rt.parent != lastSlotParent)
            rt.SetParent(lastSlotParent, worldPositionStays: true);

        SetScaleAbs(slotScale, 0f);

        rt.DOAnchorPos(lastSlotLocal, missReturnDuration)
          .SetEase(missReturnEase)
          .SetId(POS_ID)
          .OnComplete(() =>
          {
              isInSlot = true;
              FinishDragVisuals(stayScaled: true);
              state = DragState.Snapped;
          });
    }

    private void FinishDragVisuals(bool stayScaled)
    {
        if (!stayScaled) SetScaleOriginal(dragScaleDuration);
        if (handManager) handManager.SetDragging(rt, false);
    }

    private void SetScaleAbs(float target, float duration)
    {
        DOTween.Kill(SCALE_ID);
        rt.DOScale(new Vector3(target, target, target), duration)
          .SetUpdate(true)
          .SetId(SCALE_ID);
    }

    private void SetScaleOriginal(float duration)
    {
        DOTween.Kill(SCALE_ID);
        rt.DOScale(originalScale, duration)
          .SetUpdate(true)
          .SetId(SCALE_ID);
    }

    private void MoveToPointerCenter(PointerEventData e)
    {
        var parentRect = (RectTransform)rt.parent;
        if (!parentRect) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, e.position, e.pressEventCamera, out var local))
        {
            rt.anchoredPosition = local;
        }
    }

    private void OnDisable()
    {
        pointerActive = false;
        dragEnded?.Invoke();
        cardGrid?.SetDrag(null, false);

        DOTween.Kill(POS_ID);
        DOTween.Kill(SCALE_ID);
        DOTween.Kill(HAND_LAYOUT_ID);
        DOTween.Kill(GRID_POS_ID);

        state = DragState.Idle;

        if (countsForHandHover && handManager)
        {
            countsForHandHover = false;
            handManager.CardHoverExit(rt);
        }
        isPointerOver = false;
    }

    public void InitializeAsInSlot(RectTransform slotParent, Vector2 localInParent, bool setScaleToSlot = true)
    {
        lastSlotParent = slotParent;
        lastSlotLocal = localInParent;
        isInSlot = true;

        if (rt.parent != slotParent)
            rt.SetParent(slotParent, worldPositionStays: true);

        rt.anchoredPosition = localInParent;
        if (setScaleToSlot) rt.localScale = new Vector3(slotScale, slotScale, slotScale);
        state = DragState.Snapped;
    }
}
