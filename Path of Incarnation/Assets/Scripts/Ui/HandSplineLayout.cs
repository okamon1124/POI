using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;
using System.Collections.Generic;

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

    private void Awake()
    {
        if (!cardsRoot)
            cardsRoot = (RectTransform)transform;
    }

    [ContextMenu("Reflow")]
    public void Reflow()
    {
        if (!cardsRoot || !spline) return;

        // collect active UiCards under cardsRoot
        var cards = new List<UiCard>();
        foreach (Transform child in cardsRoot)
        {
            var uiCard = child.GetComponent<UiCard>();
            if (uiCard != null)
                cards.Add(uiCard);
        }

        int n = cards.Count;
        if (n == 0) return;

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
            var card = cards[i];
            if (!card) continue;

            // keep consistent visual order (0..n-1)
            card.transform.SetSiblingIndex(i);

            int j = (direction == Direction.RightToLeft) ? (n - 1 - i) : i;
            float t = (n == 1) ? start : (start + step * j);

            Vector3 pos = spline.EvaluatePosition(t);
            Quaternion rot = Quaternion.identity;

            if (rotateWithSpline)
            {
                Vector3 forward = spline.EvaluateTangent(t);
                Vector3 up = spline.EvaluateUpVector(t);
                rot = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            }

            var rt = card.GetComponent<RectTransform>();
            if (!rt) continue;

            // pivot correction so the visual center follows the spline, not the pivot
            Vector2 size = rt.rect.size;
            Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
            Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
            Vector3 targetPivotWorld = pos - worldOffset;

            rt.DOKill(true);
            rt.DOMove(targetPivotWorld, duration).SetEase(ease);
            if (rotateWithSpline)
                rt.DORotateQuaternion(rot, duration).SetEase(ease);
        }
    }
}
