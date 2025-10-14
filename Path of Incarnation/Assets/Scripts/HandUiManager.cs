using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HandUiManager : MonoBehaviour
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

    [Header("Hand Hover/Hide (UI)")]
    [SerializeField] private float handDownOffset = -150f;
    [SerializeField] private float handTweenDuration = 0.25f;

    [Header("Hand Hover Zone (optional)")]
    [Tooltip("If assigned, the mouse must be inside this rect for the hand to hover.")]
    [SerializeField] private RectTransform hoverZone;
    [Tooltip("If no hoverZone is assigned, use this percentage band from the bottom of the screen.")]
    [Range(0f, 0.5f)][SerializeField] private float fallbackHoverBand01 = 0.25f;

    private float handLift01 = 0f;
    private Tween handTween;

    private Camera CanvasCam => canvas && canvas.worldCamera ? canvas.worldCamera : Camera.main;

    private readonly List<RectTransform> handCards = new List<RectTransform>();
    private RectTransform canvasRect;
    private RectTransform currentDragged;
    private RectTransform handAnimating;

    // Interaction gate: block clicks/drags while any card is returning
    [SerializeField] private bool blockInteractionsDuringReturn = true;
    private int busyCount = 0;
    public bool IsBusy => busyCount > 0;
    public void PushBusy() { busyCount++; }
    public void PopBusy() { busyCount = Mathf.Max(0, busyCount - 1); }

    private readonly HashSet<RectTransform> hoveredCards = new HashSet<RectTransform>();
    private int hoverEpoch = 0;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("HandUiManager: Assign a Canvas set to World Space.");
            enabled = false;
            return;
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        if (splineContainer == null)
        {
            Debug.LogError("HandUiManager: Assign a SplineContainer.");
            enabled = false;
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Debug test draw key (Editor only)
        if (Input.GetKeyDown(KeyCode.Space))
            DrawCard();
    }
#endif

    // Watchdog: if a layout tween was killed, unblock layout updates
    private void LateUpdate()
    {
        if (handAnimating == null) return;

        string layoutId = $"hand_layout_{handAnimating.GetInstanceID()}";
        if (!DOTween.IsTweening(layoutId))
        {
            handAnimating = null;                // unblock SetHandHover layout updates
            UpdateCardPositions(animateAll: false);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────
    public void DrawCard()
    {
        if (handCards.Count >= maxHandSize || cardPrefab == null) return;

        GameObject g = Instantiate(cardPrefab, transform, false); // 👈 parent = HandUiManager
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

        var drag = g.GetComponent<UIDraggable>();
        if (drag != null)
            drag.handManager = this;

        handCards.Add(rect);
        handAnimating = rect;

        UpdateCardPositions(
            specific: rect,
            onSpecificDone: () => { handAnimating = null; },
            animateAll: false,
            forceAnimate: rect
        );

        currentDragged = null;
    }

    public void SetDragging(RectTransform rect, bool dragging)
    {
        currentDragged = dragging ? rect : null;
    }

    public void DetachCard(RectTransform rect)
    {
        handCards.Remove(rect);
        if (currentDragged == rect) currentDragged = null;
        UpdateCardPositions();
    }

    public void ReturnCardToHand(RectTransform rect, System.Action onLaidOut = null)
    {
        if (rect.parent != transform)
            rect.SetParent(transform, worldPositionStays: true);

        if (!handCards.Contains(rect))
            handCards.Add(rect);

        // keep left→right sibling order
        int targetIndex = handCards.IndexOf(rect);
        if (targetIndex >= 0 && targetIndex < transform.childCount)
            rect.SetSiblingIndex(targetIndex);

        handAnimating = rect;

        // Ensure this returning card can't trigger hover
        hoveredCards.Remove(rect);
        RefreshHoverFromPointer(ignore: rect);

        if (blockInteractionsDuringReturn) PushBusy();   // 👈 start block

        UpdateCardPositions(
            specific: rect,
            onSpecificDone: () =>
            {
                handAnimating = null;
                if (blockInteractionsDuringReturn) PopBusy();  // 👈 stop block
                onLaidOut?.Invoke();
            },
            animateAll: false,
            forceAnimate: rect
        );

        currentDragged = null;
    }

    public bool Contains(RectTransform rect) => handCards.Contains(rect);

    // ─────────────────────────────────────────────────────────────
    // Layout
    // ─────────────────────────────────────────────────────────────
    private void UpdateCardPositions(
        RectTransform specific = null,
        System.Action onSpecificDone = null,
        bool animateAll = true,
        RectTransform forceAnimate = null)
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstP = 0.5f - (handCards.Count - 1) * cardSpacing / 2f;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < handCards.Count; i++)
        {
            var rect = handCards[i];
            if (rect == currentDragged) continue;

            float p = Mathf.Clamp01(firstP + i * cardSpacing);
            bool animateThis = (forceAnimate != null && rect == forceAnimate) || animateAll;

            PlaceRectOnSpline(rect, spline, p,
                onSpecificDone != null && rect == specific ? onSpecificDone : null,
                animateThis);
        }
    }

    private void PlaceRectOnSpline(RectTransform rect, Spline spline, float t, System.Action onComplete, bool animate)
    {
        Vector3 worldPos = (Vector3)spline.EvaluatePosition(t);
        Vector3 worldTan = ((Vector3)spline.EvaluateTangent(t)).normalized;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 localTan = transform.InverseTransformDirection(worldTan).normalized;

        float yOffset = Mathf.Lerp(handDownOffset, 0f, handLift01);
        localPos += new Vector3(0f, yOffset, 0f);

        Vector2 dir2D = new Vector2(localTan.x, localTan.y);
        if (dir2D.sqrMagnitude < 1e-6f) dir2D = Vector2.right;
        float angleDeg = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;

        string layoutId = $"hand_layout_{rect.GetInstanceID()}";
        DOTween.Kill(layoutId);

        if (!animate)
        {
            rect.anchoredPosition3D = localPos;
            rect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence().SetId(layoutId);
        seq.Join(rect.DOAnchorPos3D(localPos, tweenDuration));
        seq.Join(rect.DOLocalRotate(new Vector3(0f, 0f, angleDeg), tweenDuration));
        if (onComplete != null) seq.OnComplete(() => onComplete());
    }

    public void SetHandHover(bool show)
    {
        float target = show ? 1f : 0f;
        if (Mathf.Approximately(handLift01, target)) return;

        if (handTween != null && handTween.IsActive()) handTween.Kill();
        handTween = DOTween
            .To(() => handLift01, v =>
            {
                handLift01 = v;

                // avoid layout jitter during card animation
                if (handAnimating == null)
                    UpdateCardPositions(animateAll: false);
            }, target, handTweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void CardHoverEnter(RectTransform card)
    {
        // record hover; any enter means we should be up
        if (hoveredCards.Add(card))
        {
            hoverEpoch++;                 // invalidate any pending drop
            SetHandHover(true);
        }
    }

    public void CardHoverExit(RectTransform card)
    {
        if (!hoveredCards.Remove(card)) return;

        int ticket = ++hoverEpoch;        // new “version” for this exit
        // Defer the decision until end of frame to allow the *new* card’s enter to arrive
        StartCoroutine(CoalesceHoverExit(ticket));
    }

    private IEnumerator CoalesceHoverExit(int ticket)
    {
        yield return null;                // wait one frame
        if (ticket != hoverEpoch) yield break;   // another enter/exit happened; cancel
        if (hoveredCards.Count == 0) SetHandHover(false);
    }

    /// <summary>
    /// Recompute hover based on current pointer pos.
    /// Only lifts if the pointer is in the hover zone AND over a hand card.
    /// </summary>
    public void RefreshHoverFromPointer(RectTransform ignore = null)
    {
        if (!EventSystem.current) return;

        Vector2 mouse = Input.mousePosition;

        // zone test
        bool inZone;
        if (hoverZone)
            inZone = RectTransformUtility.RectangleContainsScreenPoint(hoverZone, mouse, CanvasCam);
        else
            inZone = mouse.y <= (Screen.height * fallbackHoverBand01);

        bool overHandCard = false;

        if (inZone)
        {
            foreach (var rect in handCards)
            {
                if (!rect || rect == ignore) continue; // ✅ always ignore the released/returning card
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mouse, CanvasCam))
                {
                    overHandCard = true;
                    break;
                }
            }
        }

        // Don’t fight with ongoing tween
        if (handAnimating == null)
            SetHandHover(overHandCard);
        else
            DOTween.Sequence().AppendInterval(0.05f).OnComplete(() => SetHandHover(overHandCard));
    }
}
