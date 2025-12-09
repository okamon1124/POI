using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class HandSplineLayout : MonoBehaviour
{
    public enum Direction { LeftToRight, RightToLeft }

    [Header("Targets")]
    [SerializeField] private RectTransform cardsRoot;   // parent holding all UiCards
    [SerializeField] private SplineContainer spline;

    [Header("Range on spline (0..1)")]
    [SerializeField, Range(0f, 1f)] private float tStart = 0.1f;
    [SerializeField, Range(0f, 1f)] private float tEnd = 0.9f;

    [Header("Density / Fit")]
    [SerializeField, Range(0f, 0.2f)] private float edgePaddingT = 0.02f;
    [SerializeField] private bool rotateWithSpline = true;

    [Header("Order")]
    [SerializeField] private Direction direction = Direction.RightToLeft;

    [Header("Tween")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("Spacing")]
    [SerializeField, Range(0.001f, 1f)]
    private float spacingT = 0.08f;
    [SerializeField]
    private bool autoShrinkToFit = true;

    private List<UiCard> logicalOrder = new List<UiCard>();

    public enum ZOrderResetMode
    {
        ResetToSideOrder,   // Option 1: Reset to left-to-right order
        KeepLastOrder       // Option 2: Keep pyramid order from last hover
    }

    [Header("Z-Order")]
    [SerializeField] private ZOrderResetMode zOrderResetMode = ZOrderResetMode.KeepLastOrder;

    private void Awake()
    {
        if (!cardsRoot)
            cardsRoot = (RectTransform)transform;
    }

    [ContextMenu("Reflow")]
    public void Reflow()
    {
        if (!cardsRoot || !spline) return;

        // Auto-refresh logical order if empty or count mismatch
        int actualChildCount = 0;
        foreach (Transform child in cardsRoot)
        {
            if (child.GetComponent<UiCard>() != null)
                actualChildCount++;
        }

        if (logicalOrder.Count != actualChildCount)
            NotifyCardsChanged(true);  // <-- Changed: reset z-order when cards change

        int n = logicalOrder.Count;
        if (n == 0) return;

        // ... rest of Reflow uses logicalOrder instead of sibling indices ...

        // Clamp and normalize range
        float a = Mathf.Clamp01(tStart);
        float b = Mathf.Clamp01(tEnd);
        if (b < a) (a, b) = (b, a);

        // Apply edge padding
        float padA = Mathf.Lerp(a, b, edgePaddingT);
        float padB = Mathf.Lerp(b, a, edgePaddingT);
        if (padB < padA) (padA, padB) = (padB, padA);

        float usable = Mathf.Max(0.0001f, padB - padA);
        float step;
        float start;

        if (n == 1)
        {
            float mid = (padA + padB) * 0.5f;
            step = 0f;
            start = mid;
        }
        else
        {
            float desiredStep = Mathf.Clamp(spacingT, 0.0001f, usable);
            float maxStepToFit = usable / (n - 1);
            step = autoShrinkToFit ? Mathf.Min(desiredStep, maxStepToFit) : desiredStep;

            float strip = step * (n - 1);
            float mid = (padA + padB) * 0.5f;
            start = mid - strip * 0.5f;
            start = Mathf.Clamp(start, padA, padB - strip);
        }

        for (int i = 0; i < n; i++)
        {
            var card = logicalOrder[i];
            if (!card) continue;

            int j = (direction == Direction.RightToLeft) ? (n - 1 - i) : i;
            float t = (n == 1) ? start : (start + step * j);

            Vector3 pos = spline.EvaluatePosition(t);
            Vector3 posWorld = spline.transform.TransformPoint(pos);
            Quaternion rot = Quaternion.identity;

            if (rotateWithSpline)
            {
                Vector3 forward = spline.EvaluateTangent(t);
                Vector3 up = spline.EvaluateUpVector(t);
                rot = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            }

            var rt = card.GetComponent<RectTransform>();
            if (!rt) continue;

            Vector2 size = rt.rect.size;
            Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
            Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
            Vector3 targetPivotWorld = posWorld - worldOffset;

            rt.DOKill(true);
            rt.DOMove(targetPivotWorld, duration).SetEase(ease);
            if (rotateWithSpline)
                rt.DORotateQuaternion(rot, duration).SetEase(ease);
        }
    }

    /// <summary>
    /// Force reset sibling order to match left-to-right positional order.
    /// Call this when you need to normalize Z-order (e.g., after all hovers end).
    /// </summary>
    [ContextMenu("Reset Sibling Order")]
    public void ResetSiblingOrder()
    {
        if (!cardsRoot) return;

        var cards = new List<UiCard>();
        foreach (Transform child in cardsRoot)
        {
            var uiCard = child.GetComponent<UiCard>();
            if (uiCard != null)
                cards.Add(uiCard);
        }

        // Sort by current sibling index, then set them sequentially
        cards = cards.OrderBy(c => c.transform.GetSiblingIndex()).ToList();

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(i);
        }
    }

    public void RefreshLogicalOrder()
    {
        logicalOrder.Clear();

        // Collect all cards
        var cards = new List<UiCard>();
        foreach (Transform child in cardsRoot)
        {
            var uiCard = child.GetComponent<UiCard>();
            if (uiCard != null)
                cards.Add(uiCard);
        }

        // Sort by the MODEL's slot index, not sibling index
        logicalOrder = cards
            .Where(c => c.cardInstance?.CurrentSlot != null)
            .OrderBy(c => c.cardInstance.CurrentSlot.Index)
            .ToList();

        // Always sync sibling indices to match logical order immediately
        for (int i = 0; i < logicalOrder.Count; i++)
        {
            if (logicalOrder[i] != null)
                logicalOrder[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// Apply pyramid z-order centered on the hovered card.
    /// Call with null to reset based on zOrderResetMode.
    /// </summary>
    public void ApplyPyramidZOrder(UiCard hoveredCard)
    {
        if (!cardsRoot) return;

        // Ensure we have logical order
        int actualChildCount = 0;
        foreach (Transform child in cardsRoot)
        {
            if (child.GetComponent<UiCard>() != null)
                actualChildCount++;
        }

        bool cardsChanged = logicalOrder.Count != actualChildCount;
        if (cardsChanged)
        {
            // Reset z-order first, then we'll apply pyramid below
            NotifyCardsChanged(true);
        }

        if (hoveredCard == null)
        {
            // Reset based on mode
            if (zOrderResetMode == ZOrderResetMode.ResetToSideOrder)
            {
                for (int i = 0; i < logicalOrder.Count; i++)
                {
                    if (logicalOrder[i] != null)
                        logicalOrder[i].transform.SetSiblingIndex(i);
                }
            }
            // KeepLastOrder: do nothing, keep current sibling order
            return;
        }

        // Find hovered card's logical index
        int hoveredLogicalIndex = logicalOrder.IndexOf(hoveredCard);
        if (hoveredLogicalIndex < 0) return;

        // Create list sorted by distance from hovered (furthest first = behind)
        var sorted = logicalOrder
            .Select((card, index) => (card, distance: Mathf.Abs(index - hoveredLogicalIndex)))
            .OrderByDescending(x => x.distance)
            .ThenBy(x => logicalOrder.IndexOf(x.card))
            .Select(x => x.card)
            .ToList();

        // Apply new sibling order
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i] != null)
                sorted[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// Call when cards are added or removed from hand.
    /// </summary>
    /// <param name="forceResetZOrder">True when cards added/removed, resets z-order to side-to-side</param>
    public void NotifyCardsChanged(bool forceResetZOrder = true)
    {
        RefreshLogicalOrder();

        if (forceResetZOrder)
        {
            // Reset sibling order to match logical order (left-to-right)
            for (int i = 0; i < logicalOrder.Count; i++)
            {
                if (logicalOrder[i] != null)
                    logicalOrder[i].transform.SetSiblingIndex(i);
            }
        }
    }
}