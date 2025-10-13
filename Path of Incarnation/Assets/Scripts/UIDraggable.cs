using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    // ─────────────────────────────────────────────────────────────
    // Runtime references
    // ─────────────────────────────────────────────────────────────
    private CardGrid cardGrid; // found at runtime

    [Header("Snap")]
    [Tooltip("If true, reparent to the grid container before positioning in a slot.")]
    [SerializeField] private bool reparentOnSnap = true;

    [Header("Hand (optional)")]
    [Tooltip("Assign if this card participates in a world-space hand layout.")]
    public HandManagerUIWorldSpace handManager;

    [Header("Drag / Slot Visuals")]
    [Tooltip("ABSOLUTE scale while dragging or sitting in a slot.")]
    [SerializeField] private float slotScale = 1.1f;
    [SerializeField] private float dragScaleDuration = 0.08f;

    [Header("Miss Return (slot→slot)")]
    [SerializeField] private float missReturnDuration = 0.18f;
    [SerializeField] private Ease missReturnEase = Ease.OutQuad;

    // ─────────────────────────────────────────────────────────────
    // Internal state
    // ─────────────────────────────────────────────────────────────
    private RectTransform rt;
    private bool returning;
    private bool canDrag = true;
    private bool lightIsDown;
    private bool pointerActive;           // debounces rapid taps

    public Action dragStarted;
    public Action dragEnded;

    private Tween moveTween;              // position tween
    private Tween scaleTween;             // scale tween (dedicated, to avoid races)
    private Vector3 originalScale;

    // Track where the drag started
    private bool cameFromHand;

    // Remember last snapped slot pose (parent + local)
    private RectTransform lastSlotParent;
    private Vector2 lastSlotLocal;
    private bool isInSlot;

    private RectTransform CurrentParent => (RectTransform)rt.parent;

    // ─────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        originalScale = rt.localScale;
        ResolveRuntimeRefs();
    }

    private void OnTransformParentChanged() => ResolveRuntimeRefs();

    /// <summary>Resolve CardGrid reference robustly.</summary>
    public void ResolveRuntimeRefs()
    {
        // Prefer parent chain (covers case where card lives under a grid root)
        cardGrid = transform.parent ? transform.parent.GetComponentInChildren<CardGrid>() : null;

        // Same-level (siblings) — look under our immediate parent only; pick closest if multiple
        if (cardGrid == null && transform.parent)
        {
            CardGrid best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (Transform child in transform.parent)
            {
                if (child == transform) continue;
                var cg = child.GetComponent<CardGrid>();
                if (cg == null) continue;
                float d2 = (child.position - transform.position).sqrMagnitude;
                if (d2 < bestSqr) { bestSqr = d2; best = cg; }
            }
            cardGrid = best;
        }

        // Last resort: scene search
        if (cardGrid == null)
            cardGrid = FindFirstObjectByType<CardGrid>();

        if (cardGrid == null)
            Debug.LogWarning($"{name}: No CardGrid found (parent/sibling/scene).");
    }

    // ─────────────────────────────────────────────────────────────
    // Pointer / Drag Interfaces
    // ─────────────────────────────────────────────────────────────
    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        // Ignore taps during a return animation; also debounce rapid repeated downs
        if (returning || pointerActive) return;
        pointerActive = true;

        if (!lightIsDown)
        {
            dragStarted?.Invoke();
            lightIsDown = true;
        }

        KillMoveOnly();
        rt.SetAsLastSibling(); // bring on top

        // Origin: hand or slot/field?
        cameFromHand = handManager != null && handManager.Contains(rt);
        if (cameFromHand) handManager.SetDragging(rt, true);

        // Drag visuals: ABSOLUTE scale and upright
        SetScaleAbs(slotScale, dragScaleDuration);
        rt.localRotation = Quaternion.Euler(0f, 0f, 0f);

        if (cardGrid) cardGrid.SetDrag(rt, true);

        MoveToPointerCenter(e);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!canDrag || returning) return;
        MoveToPointerCenter(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!canDrag || returning) return;
        MoveToPointerCenter(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!canDrag || returning) return;

        if (cardGrid != null &&
            cardGrid.TrySnapFromDraggable(rt, reparentOnSnap, out var snappedParent, out var snappedLocal))
        {
            OnSnappedToSlot(snappedParent, snappedLocal);
            return;
        }

        OnDropMissed();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (returning) return;        // ignore ups during return
        if (!pointerActive) return;   // debounce: ignore stray ups
        pointerActive = false;

        if (lightIsDown)
        {
            dragEnded?.Invoke();
            lightIsDown = false;
        }

        if (cardGrid) cardGrid.SetDrag(null, false);
        if (returning) return;

        if (cardGrid != null &&
            cardGrid.TrySnapFromDraggable(rt, reparentOnSnap, out var snappedParent, out var snappedLocal))
        {
            OnSnappedToSlot(snappedParent, snappedLocal);
            return;
        }

        OnDropMissed();
    }

    // ─────────────────────────────────────────────────────────────
    // Core behaviors
    // ─────────────────────────────────────────────────────────────
    private void OnSnappedToSlot(RectTransform snappedParent, Vector2 snappedLocal)
    {
        if (cameFromHand && handManager != null)
            handManager.DetachCard(rt);

        lastSlotParent = snappedParent;
        lastSlotLocal = snappedLocal;
        isInSlot = true;

        // Keep ABSOLUTE slot scale (1.1)
        FinishDragVisuals(stayScaled: true);
    }

    private void OnDropMissed()
    {
        // Robust decision: if the hand owns this card (now or originally), go back to hand
        bool shouldReturnToHand = handManager != null && (cameFromHand || handManager.Contains(rt));

        if (shouldReturnToHand)
        {
            ReturnToHand();
        }
        else if (isInSlot && lastSlotParent != null)
        {
            ReturnToLastSlot();
        }
        else
        {
            FinishDragVisuals(stayScaled: false);
        }
    }

    private void ReturnToHand()
    {
        returning = true;
        canDrag = false;

        KillMoveOnly();

        // allow hand to control position/rotation, but we restore scale now
        SetScaleOriginal(dragScaleDuration);

        // IMPORTANT: let the hand layout move this card again before requesting return
        if (handManager != null)
            handManager.SetDragging(rt, false);

        handManager.ReturnCardToHand(rt, onLaidOut: () =>
        {
            returning = false;
            canDrag = true;
            isInSlot = false;
            FinishDragVisuals(stayScaled: false);
        });
    }

    private void ReturnToLastSlot()
    {
        returning = true;
        canDrag = false;

        KillMoveOnly();

        if (rt.parent != lastSlotParent)
            rt.SetParent(lastSlotParent, worldPositionStays: true);

        // ensure ABSOLUTE slot scale in slot
        SetScaleAbs(slotScale, 0f);

        moveTween = rt
            .DOAnchorPos(lastSlotLocal, missReturnDuration)
            .SetEase(missReturnEase)
            .OnComplete(() =>
            {
                returning = false;
                canDrag = true;
                isInSlot = true;
                FinishDragVisuals(stayScaled: true); // remain 1.1 in slot
            });
    }

    private void FinishDragVisuals(bool stayScaled)
    {
        if (!stayScaled) SetScaleOriginal(dragScaleDuration);

        if (handManager != null)
            handManager.SetDragging(rt, false);
    }

    // ─────────────────────────────────────────────────────────────
    // Scale utilities (race-safe)
    // ─────────────────────────────────────────────────────────────
    private void SetScaleAbs(float target, float duration)
    {
        // kill only the previous scale tween; leave move tween alone
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        // absolute uniform target
        scaleTween = rt.DOScale(new Vector3(target, target, target), duration).SetUpdate(true);
    }

    private void SetScaleOriginal(float duration)
    {
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        scaleTween = rt.DOScale(originalScale, duration).SetUpdate(true);
    }

    // ─────────────────────────────────────────────────────────────
    // Motion utilities
    // ─────────────────────────────────────────────────────────────
    private void KillMoveOnly()
    {
        if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        // Intentionally do NOT call DOTween.Kill(rt) here; that would also kill the scale tween.
    }

    private void ResetStateAndKillAllTweens()
    {
        returning = false;
        canDrag = true;

        if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();

        DOTween.Kill(rt); // final cleanup (only on disable)
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

        if (lightIsDown)
        {
            dragEnded?.Invoke();
            lightIsDown = false;
        }
        if (cardGrid) cardGrid.SetDrag(null, false);

        ResetStateAndKillAllTweens();
    }

    // ─────────────────────────────────────────────────────────────
    // Optional: pre-seed slot state for cards that start in a slot
    // ─────────────────────────────────────────────────────────────
    public void InitializeAsInSlot(RectTransform slotParent, Vector2 localInParent, bool setScaleToSlot = true)
    {
        lastSlotParent = slotParent;
        lastSlotLocal = localInParent;
        isInSlot = true;

        if (rt.parent != slotParent)
            rt.SetParent(slotParent, worldPositionStays: true);

        rt.anchoredPosition = localInParent;
        if (setScaleToSlot) rt.localScale = new Vector3(slotScale, slotScale, slotScale);
    }
}
