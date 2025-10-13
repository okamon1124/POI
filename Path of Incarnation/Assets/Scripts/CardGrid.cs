using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Put this on the "Grids" parent. It auto-collects its child RectTransforms
/// as snap targets and snaps a card (RectTransform) to the nearest target
/// when within snapDistance (in screen pixels).
/// </summary>
[DisallowMultipleComponent]
public class CardGrid : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("If empty, all direct children of this transform will be used as snap targets.")]
    [SerializeField] private List<RectTransform> snapTargets = new List<RectTransform>();

    [Header("Snap Settings")]
    [Tooltip("Max distance (in screen pixels) between the card center and a cell center to allow snapping.")]
    [SerializeField] private float snapDistance = 80f;

    [SerializeField] private bool animateSnap = true;
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private Ease snapEase = Ease.OutQuad;

    [Header("References (optional)")]
    [Tooltip("Canvas used for screen <-> local conversions. If null, the first parent Canvas will be used.")]
    [SerializeField] private Canvas rootCanvas;

    public RectTransform CurrentDraggedCard { get; private set; }
    public bool IsDragging { get; private set; }

    private Camera UICamera => rootCanvas ? rootCanvas.worldCamera : null;

    private void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        AutoCollectIfEmpty();
    }

    public void SetDrag(RectTransform card, bool dragging)
    {
        IsDragging = dragging;
        CurrentDraggedCard = dragging ? card : null;
    }

    /// <summary>
    /// Call this from UIDraggable when the user releases the card.
    /// If a close target is found, the card is snapped there and the method returns true.
    /// </summary>
    /// <param name="cardRT">RectTransform of the draggable card.</param>
    /// <param name="reparentOnSnap">
    /// If true, the card is reparented to the grid container (this.transform) before positioning,
    /// which avoids anchor/scale issues if the card lived elsewhere.
    /// </param>
    public bool TrySnapFromDraggable(RectTransform cardRT, bool reparentOnSnap = false)
    {
        if (!cardRT || snapTargets == null || snapTargets.Count == 0 || !rootCanvas)
            return false;

        // 1) Card center in screen space
        Vector2 cardScreen = RectCenterScreen(cardRT);

        // 2) Find nearest target in screen space
        RectTransform best = null;
        float bestDist = float.MaxValue;

        foreach (var cell in snapTargets)
        {
            if (!cell || !cell.gameObject.activeInHierarchy) continue;

            float d = Vector2.Distance(cardScreen, RectCenterScreen(cell));
            if (d < bestDist)
            {
                bestDist = d;
                best = cell;
            }
        }

        // 3) Too far? No snap.
        if (!best || bestDist > snapDistance)
            return false;

        // 4) Compute destination: center of the chosen target, expressed in the parent we'll use
        RectTransform parentForLocal = reparentOnSnap
            ? (RectTransform)transform // grid container as parent
            : (RectTransform)cardRT.parent;

        // Reparent first if requested (keep world position so conversions are consistent)
        if (reparentOnSnap && cardRT.parent != parentForLocal)
            cardRT.SetParent(parentForLocal, worldPositionStays: true);

        Vector2 targetScreen = RectCenterScreen(best);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentForLocal, targetScreen, UICamera, out var local))
        {
            DOTween.Kill(cardRT); // stop any ongoing move on this card

            if (animateSnap)
            {
                cardRT.DOAnchorPos(local, snapDuration)
                      .SetEase(snapEase)
                      .SetTarget(cardRT);
            }
            else
            {
                cardRT.anchoredPosition = local;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Re-scan children for snap targets (call this if you add/remove children at runtime).
    /// </summary>
    public void ForceRefreshTargets()
    {
        AutoCollectIfEmpty(force: true);
    }

    private void AutoCollectIfEmpty(bool force = false)
    {
        if (snapTargets == null) snapTargets = new List<RectTransform>();
        if (!force && snapTargets.Count > 0) return;

        snapTargets.Clear();
        foreach (Transform child in transform)
        {
            if (child is RectTransform rtChild)
                snapTargets.Add(rtChild);
        }
    }

    private Vector2 RectCenterScreen(RectTransform rt)
    {
        // local rect center -> world -> screen
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        return RectTransformUtility.WorldToScreenPoint(UICamera, worldCenter);
    }

#if UNITY_EDITOR
    // Optional gizmos so you can see the centers (approximate)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform child in transform)
        {
            if (child is RectTransform rt)
            {
                Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
                Gizmos.DrawWireSphere(worldCenter, 0.02f);
            }
        }
    }
#endif
}
