using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class CardGrid : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("If empty, all direct children are used as snap targets.")]
    [SerializeField] private List<RectTransform> snapTargets = new List<RectTransform>();

    [Header("Snap Settings")]
    [Tooltip("Max distance (screen pixels) between card center and cell center to allow snapping.")]
    [SerializeField] private float snapDistance = 80f;
    [SerializeField] private bool animateSnap = true;
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private Ease snapEase = Ease.OutQuad;

    [Header("References (optional)")]
    [Tooltip("Canvas used for screen <-> local conversions. Falls back to first parent Canvas.")]
    [SerializeField] private Canvas rootCanvas;

    public RectTransform CurrentDraggedCard { get; private set; }
    public bool IsDragging { get; private set; }

    private Camera uiCam;
    private string PosId(RectTransform rt) => $"grid_pos_{rt.GetInstanceID()}";

    private void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (!rootCanvas)
        {
            Debug.LogError("CardGrid: No Canvas found in parents.");
            enabled = false; return;
        }

        uiCam = rootCanvas.worldCamera; // can be null for Overlay
        AutoCollectIfEmpty();
    }

    public void SetDrag(RectTransform card, bool dragging)
    {
        IsDragging = dragging;
        CurrentDraggedCard = dragging ? card : null;
    }

    /// <summary>Convenience overload.</summary>
    public bool TrySnapFromDraggable(RectTransform cardRT, bool reparentOnSnap = false)
        => TrySnapFromDraggable(cardRT, reparentOnSnap, out _, out _);

    /// <summary>Find nearest target and snap if within snapDistance.</summary>
    public bool TrySnapFromDraggable(RectTransform cardRT, bool reparentOnSnap, out RectTransform snappedParent, out Vector2 snappedLocal)
    {
        snappedParent = null; snappedLocal = Vector2.zero;
        if (!cardRT || snapTargets == null || snapTargets.Count == 0 || !rootCanvas) return false;

        // Card center in screen space
        Vector2 cardScreen = RectCenterScreen(cardRT);

        // Nearest active target
        RectTransform best = null; float bestDist = float.MaxValue;
        for (int i = 0; i < snapTargets.Count; i++)
        {
            var cell = snapTargets[i];
            if (!cell || !cell.gameObject.activeInHierarchy) continue;

            float d = Vector2.Distance(cardScreen, RectCenterScreen(cell));
            if (d < bestDist) { bestDist = d; best = cell; }
        }
        if (!best || bestDist > snapDistance) return false;

        // Compute local destination in chosen parent
        RectTransform parentForLocal = reparentOnSnap ? (RectTransform)transform : (RectTransform)cardRT.parent;
        if (reparentOnSnap && cardRT.parent != parentForLocal)
            cardRT.SetParent(parentForLocal, worldPositionStays: true);

        Vector2 targetScreen = RectCenterScreen(best);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentForLocal, targetScreen, uiCam, out var local))
            return false;

        DOTween.Kill(PosId(cardRT));
        snappedParent = parentForLocal;
        snappedLocal = local;

        if (animateSnap)
            cardRT.DOAnchorPos(local, snapDuration).SetEase(snapEase).SetId(PosId(cardRT));
        else
            cardRT.anchoredPosition = local;

        return true;
    }

    /// <summary>Re-scan children for snap targets.</summary>
    public void ForceRefreshTargets() => AutoCollectIfEmpty(force: true);

    private void AutoCollectIfEmpty(bool force = false)
    {
        if (snapTargets == null) snapTargets = new List<RectTransform>();
        if (!force && snapTargets.Count > 0) return;

        snapTargets.Clear();
        foreach (Transform child in transform)
            if (child is RectTransform rt) snapTargets.Add(rt);
    }

    private Vector2 RectCenterScreen(RectTransform rt)
    {
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        return RectTransformUtility.WorldToScreenPoint(uiCam, worldCenter);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform child in transform)
            if (child is RectTransform rt)
                Gizmos.DrawWireSphere(rt.TransformPoint(rt.rect.center), 0.02f);
    }
#endif
}
