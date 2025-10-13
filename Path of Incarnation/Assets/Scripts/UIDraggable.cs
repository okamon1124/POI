using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    [Header("Return Animation")]
    [SerializeField] private float timeToReturn = 0.25f;
    [SerializeField] private Ease returnEase = Ease.OutQuad;

    [Header("Snap")]
    [Tooltip("Assign the Grids object (with CardGrid). Only one Card is dragged, many slots exist.")]
    [SerializeField] private CardGrid cardGrid;
    [Tooltip("If true, reparent the card to the Grids object before snapping to avoid anchor/scale issues.")]
    [SerializeField] private bool reparentOnSnap = false;

    private RectTransform rt;
    private RectTransform parentRect;
    private Canvas canvas;
    private Vector2 startAnchoredPos;
    private bool returning;
    private bool canDrag = true;

    // Light dim/bright hooks for your CardSlot
    private bool lightIsDown;
    public Action dragStarted;
    public Action dragEnded;

    private Tween returnTween;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        parentRect = rt.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        startAnchoredPos = rt.anchoredPosition;
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!lightIsDown)
        {
            dragStarted?.Invoke();
            lightIsDown = true;
        }

        if (returning) CancelReturn();

        // NEW: tell the grid a drag has started
        if (cardGrid) cardGrid.SetDrag(rt, true);

        MoveToPointerCenter(e);
        if (!canDrag) return;
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

        // Try snapping to nearest grid; if we snapped, we're done.
        if (cardGrid != null && cardGrid.TrySnapFromDraggable(rt, reparentOnSnap))
            return;

        StartReturn();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (lightIsDown)
        {
            dragEnded?.Invoke();
            lightIsDown = false;
        }

        // NEW: drag ended
        if (cardGrid) cardGrid.SetDrag(null, false);

        if (returning) return;

        if (cardGrid != null && cardGrid.TrySnapFromDraggable(rt, reparentOnSnap))
            return;

        StartReturn();
    }

    private void StartReturn()
    {
        returning = true;
        canDrag = false;

        returnTween?.Kill();

        // Smooth return to the original anchored position
        returnTween = rt.DOAnchorPos(startAnchoredPos, timeToReturn)
            .SetEase(returnEase)
            .SetTarget(rt)
            .OnComplete(() =>
            {
                returning = false;
                canDrag = true;
            });
    }

    private void CancelReturn()
    {
        returning = false;
        canDrag = true;
        returnTween?.Kill();
    }

    private void MoveToPointerCenter(PointerEventData e)
    {
        // Convert to local space of the current parent to set anchoredPosition correctly
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, e.position, e.pressEventCamera, out var local))
        {
            rt.anchoredPosition = local;
        }
    }

    private void OnDisable()
    {
        if (lightIsDown)
        {
            dragEnded?.Invoke();
            lightIsDown = false;
        }
        if (cardGrid) cardGrid.SetDrag(null, false);
    }
}
