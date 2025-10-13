using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class CardSlotLight : MonoBehaviour
{
    [Header("References")]
    private CardGrid cardGrid;   // ← assign the Grids object here
    private Light2D light2d;
    private RectTransform slotRect;
    private Canvas rootCanvas;

    [Header("Intensity Levels")]
    [SerializeField] private float idleIntensity = 0f;
    [SerializeField] private float draggingIntensity = 2f;
    [SerializeField] private float hoverIntensity = 4f;
    [SerializeField] private float hoverDistance = 100f;  // screen pixels

    [Header("Tween Settings")]
    [SerializeField] private float tweenDuration = 0.25f;
    [SerializeField] private Ease tweenEase = Ease.OutQuad;

    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        light2d = GetComponent<Light2D>();
        slotRect = transform.parent.GetComponent<RectTransform>(); // parent = the slot
        rootCanvas = GetComponentInParent<Canvas>();

        cardGrid = GetComponentInParent<CardGrid>();

        if (light2d != null)
            light2d.intensity = idleIntensity;
    }

    private void Update()
    {
        if (debugMode)
        {
            Debug.Log($"slotRect: {slotRect}");
            Debug.Log($"cardGrid.IsDragging: {cardGrid && cardGrid.IsDragging}");
        }

        if (cardGrid == null || slotRect == null || rootCanvas == null)
        {
            return;
        }

        // If nothing is being dragged → idle
        if (!cardGrid.IsDragging || cardGrid.CurrentDraggedCard == null)
        {
            TweenTo(idleIntensity);
            return;
        }

        // Measure distance between THIS slot center and the CURRENT dragged card center (screen space)
        Vector2 slotScreen = RectCenterScreen(slotRect);
        Vector2 cardScreen = RectCenterScreen(cardGrid.CurrentDraggedCard);
        float distance = Vector2.Distance(slotScreen, cardScreen);

        if (debugMode) Debug.Log($"distance to dragged card: {distance}");

        if (distance < hoverDistance)
            TweenTo(hoverIntensity);
        else
            TweenTo(draggingIntensity);
    }

    private Vector2 RectCenterScreen(RectTransform rt)
    {
        var cam = rootCanvas ? rootCanvas.worldCamera : null; // null is fine for Overlay canvas
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        return RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
    }

    private void TweenTo(float target)
    {
        if (light2d == null) return;

        DOTween.Kill(light2d);
        DOTween.To(() => light2d.intensity,
                   x => light2d.intensity = x,
                   target,
                   tweenDuration)
               .SetEase(tweenEase)
               .SetTarget(light2d);
    }
}
