using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

[RequireComponent(typeof(Zone))]
public class HandSplineLayout : MonoBehaviour
{
    public enum Direction { LeftToRight, RightToLeft }   // NEW

    [SerializeField] private Zone zone;
    [SerializeField] private SplineContainer spline;

    [Header("Range on spline (0..1)")]
    [SerializeField, Range(0f, 1f)] private float tStart = 0.1f;
    [SerializeField, Range(0f, 1f)] private float tEnd = 0.9f;

    [Header("Density / Fit")]
    //[SerializeField, Min(2)] private int cardsAtFullWidth = 10;
    [SerializeField, Range(0f, 0.2f)] private float edgePaddingT = 0.02f;
    [SerializeField] private bool rotateWithSpline = true;

    [Header("Order")]
    [SerializeField] private Direction direction = Direction.RightToLeft;   // default to your desired direction

    [Header("Tween")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("Spacing")]
    [SerializeField, Range(0.001f, 1f)]
    private float spacingT = 0.08f;       // desired gap between neighbors in t-space
    [SerializeField]
    private bool autoShrinkToFit = true;  // if true, spacing shrinks so all cards fit


    private void Awake() { if (!zone) zone = GetComponent<Zone>(); }
    private void OnEnable() { if (zone) zone.OccupantsChanged += OnChanged; Reflow(); }
    private void OnDisable() { if (zone) zone.OccupantsChanged -= OnChanged; }
    private void OnChanged(Zone _) => Reflow();

    [ContextMenu("Reflow")]
    public void Reflow()
    {
        if (!zone || !spline) return;
        var cards = zone.Occupants;

        float a = Mathf.Clamp01(tStart);
        float b = Mathf.Clamp01(tEnd);
        if (b < a) (a, b) = (b, a);

        float padA = Mathf.Lerp(a, b, edgePaddingT);
        float padB = Mathf.Lerp(b, a, edgePaddingT);
        if (padB < padA) (padA, padB) = (padB, padA);
        float usable = Mathf.Max(0.0001f, padB - padA);

        int n = cards.Count;
        if (n == 1) { /* place at center as you already do */ }

        // desired step from inspector
        float desiredStep = Mathf.Clamp(spacingT, 0.0001f, usable);

        // max step that still fits n cards in the usable window
        float maxStepToFit = usable / (n - 1);

        // final step
        float step = autoShrinkToFit ? Mathf.Min(desiredStep, maxStepToFit) : desiredStep;

        // compute start so the whole strip is centered, then clamp to stay inside
        float strip = step * (n - 1);
        float mid = (padA + padB) * 0.5f;
        float start = mid - strip * 0.5f;
        start = Mathf.Clamp(start, padA, padB - strip);

        for (int i = 0; i < n; i++)
        {
            var card = cards[i];
            if (!card) continue;

            // keep render order in sync with logical order
            card.transform.SetSiblingIndex(i);

            // map logical index ¡÷ visual placement index
            int j = (direction == Direction.RightToLeft) ? (n - 1 - i) : i;

            float t = start + step * j;
            Vector3 pos = spline.EvaluatePosition(t);
            Quaternion rot = Quaternion.identity;
            if (rotateWithSpline)
            {
                Vector3 forward = spline.EvaluateTangent(t);
                Vector3 up = spline.EvaluateUpVector(t);
                rot = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            }

            var rt = card.GetComponent<RectTransform>();
            Vector2 size = rt.rect.size;
            Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
            Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
            Vector3 targetPivotWorld = pos - worldOffset;

            rt.DOKill(true);
            rt.DOMove(targetPivotWorld, duration).SetEase(ease);
            if (rotateWithSpline) rt.DORotateQuaternion(rot, duration).SetEase(ease);
        }
    }
}
