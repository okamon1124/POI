using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class HandManagerUIWorldSpace : MonoBehaviour
{
    [Header("Hand")]
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private GameObject cardPrefab;

    [Header("Layout")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float tweenDuration = 0.25f;

    [Header("UI (World Space)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform spawnPoint;

    private readonly List<RectTransform> handCards = new List<RectTransform>();
    private RectTransform canvasRect;

    // Track the currently dragged card so UpdateCardPositions won’t move it.
    private RectTransform currentDragged;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("HandManagerUIWorldSpace: Assign a Canvas set to World Space.");
            enabled = false;
            return;
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        if (splineContainer == null)
        {
            Debug.LogError("HandManagerUIWorldSpace: Assign a SplineContainer.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            DrawCard();
    }

    public void DrawCard()
    {
        if (handCards.Count >= maxHandSize || cardPrefab == null) return;

        GameObject g = Instantiate(cardPrefab, canvas.transform, false);
        RectTransform rect = g.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("Card prefab must have a RectTransform.");
            Destroy(g);
            return;
        }

        // keep prefab’s local scale
        RectTransform prefabRect = cardPrefab.GetComponent<RectTransform>();
        if (prefabRect != null)
            rect.localScale = prefabRect.localScale;

        if (spawnPoint != null)
        {
            rect.anchoredPosition3D = spawnPoint.anchoredPosition3D;
            rect.localRotation = spawnPoint.localRotation;
        }
        else
        {
            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
        }

        // Wire up UIDraggable to this hand (optional but recommended)
        var drag = g.GetComponent<UIDraggable>();
        if (drag != null)
        {
            drag.handManager = this;
            // If you have a CardGrid in your scene, also assign drag.cardGrid in prefab or here.
        }

        handCards.Add(rect);
        UpdateCardPositions();
    }

    /// <summary>Called by UIDraggable to mark a card as being dragged.</summary>
    public void SetDragging(RectTransform rect, bool dragging)
    {
        currentDragged = dragging ? rect : null;
    }

    /// <summary>Called by UIDraggable when a card successfully snaps to a field slot.</summary>
    public void DetachCard(RectTransform rect)
    {
        // It is not a hand element anymore.
        handCards.Remove(rect);
        if (currentDragged == rect) currentDragged = null;
        UpdateCardPositions();
    }

    /// <summary>
    /// Called by UIDraggable when a drop missed all slots and we should return the card to the hand.
    /// Animates back into the hand layout (including rotation), then invokes onLaidOut.
    /// </summary>
    public void ReturnCardToHand(RectTransform rect, System.Action onLaidOut = null)
    {
        // Ensure it’s parented to the hand canvas
        if (rect.parent != canvas.transform)
            rect.SetParent(canvas.transform, worldPositionStays: true);

        if (!handCards.Contains(rect))
            handCards.Add(rect);

        // We’ll animate to the spline pose in UpdateCardPositions.
        // When the animation completes for this rect, call onLaidOut.
        UpdateCardPositions(rect, onLaidOut);

        // Done dragging from the hand’s perspective (we’ll clear in UIDraggable after animations)
        currentDragged = null;
    }

    public bool Contains(RectTransform rect) => handCards.Contains(rect);

    private void UpdateCardPositions(RectTransform specific = null, System.Action onSpecificDone = null)
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstP = 0.5f - (handCards.Count - 1) * cardSpacing / 2f;

        Spline spline = splineContainer.Spline;

        for (int i = 0; i < handCards.Count; i++)
        {
            var rect = handCards[i];
            // Don’t move the one we’re currently dragging
            if (rect == currentDragged) continue;

            float p = Mathf.Clamp01(firstP + i * cardSpacing);
            PlaceRectOnSpline(rect, spline, p, onSpecificDone != null && rect == specific ? onSpecificDone : null);
        }
    }

    /// <summary>
    /// Place UI card on the World Space Canvas and tween to target.
    /// </summary>
    private void PlaceRectOnSpline(RectTransform rect, Spline spline, float p, System.Action onComplete = null)
    {
        Vector3 worldPos = (Vector3)spline.EvaluatePosition(p);
        Vector3 worldTangent = ((Vector3)spline.EvaluateTangent(p)).normalized;

        Vector3 localPosOnCanvas = canvasRect.InverseTransformPoint(worldPos);
        Vector3 localTangent = canvasRect.InverseTransformDirection(worldTangent).normalized;

        Vector2 dir2D = new Vector2(localTangent.x, localTangent.y);
        if (dir2D.sqrMagnitude < 1e-6f)
            dir2D = Vector2.right;

        float angleDeg = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;

        // Position + rotation back into the curved hand
        rect.DOAnchorPos3D(localPosOnCanvas, tweenDuration);
        rect.DOLocalRotate(new Vector3(0f, 0f, angleDeg), tweenDuration)
            .OnComplete(() => onComplete?.Invoke());
    }
}
