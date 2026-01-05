using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// Single mana orb with Available / Spent / PreviewAffordable / PreviewUnaffordable states.
/// Handles brightness & breathing animations.
/// </summary> 
public class ManaOrb : MonoBehaviour
{
    public enum OrbState
    {
        Available,
        Spent,
        PreviewAffordable,
        PreviewUnaffordable
    }

    [Header("Components")]
    [SerializeField] private Image orbImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Light2D orbLight;

    [Header("Brightness")]
    [SerializeField] private float availableBrightness = 1.0f;
    [SerializeField] private float spentBrightness = 0.3f;
    [SerializeField] private float previewAffordableMin = 0.5f;
    [SerializeField] private float previewAffordableMax = 1.0f;
    [SerializeField] private float previewUnaffordableMin = 0.2f;
    [SerializeField] private float previewUnaffordableMax = 0.6f;

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private float breathingSpeed = 1.0f;
    [SerializeField] private Ease brightnessEase = Ease.OutQuad;
    [SerializeField] private Ease breathingEase = Ease.InOutSine;

    [Header("Scale Breathing")]
    [SerializeField] private float breathingScaleMax = 1.05f;

    private OrbState _currentState = OrbState.Spent;

    private Tween _brightnessTween;
    private Tween _breathingTween;
    private Tween _scaleTween;
    private Tween _lightTween;
    private Tween _lightBreathingTween;

    private float _baseLightIntensity = 1f;

    // ---------------- Initialization ----------------

    private void Awake()
    {
        if (!orbImage) orbImage = GetComponent<Image>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (!orbLight) orbLight = GetComponentInChildren<Light2D>();

        if (orbLight)
            _baseLightIntensity = orbLight.intensity;

        SetStateImmediate(OrbState.Spent);
    }

    // ---------------- Public API ----------------

    public void SetState(OrbState newState)
    {
        bool newIsPreview = IsPreview(newState);
        bool currentIsPreview = IsPreview(_currentState);

        // Preview: always restart breathing
        if (newIsPreview)
        {
            _currentState = newState;
            KillAllTweens();
            StartBreathing();
            return;
        }

        // Non-preview: no change
        if (!currentIsPreview && _currentState == newState)
            return;

        KillAllTweens();
        _currentState = newState;

        // Leaving preview? restore scale
        if (currentIsPreview)
            RestoreScale();

        AnimateBrightness(GetBrightnessForState(newState));
    }

    public void SetStateImmediate(OrbState newState)
    {
        KillAllTweens();
        _currentState = newState;

        float b = GetBrightnessForState(newState);
        canvasGroup.alpha = b;

        if (orbLight)
            orbLight.intensity = _baseLightIntensity * b;

        transform.localScale = Vector3.one;
    }

    public void StopBreathingImmediate()
    {
        _breathingTween?.Kill();
        _scaleTween?.Kill();
        _lightBreathingTween?.Kill();
        transform.localScale = Vector3.one;
    }

    // ---------------- Internal Logic ----------------

    private bool IsPreview(OrbState s) =>
        s == OrbState.PreviewAffordable || s == OrbState.PreviewUnaffordable;

    private float GetBrightnessForState(OrbState s)
    {
        return s switch
        {
            OrbState.Available => availableBrightness,
            OrbState.Spent => spentBrightness,
            OrbState.PreviewAffordable => previewAffordableMax,
            OrbState.PreviewUnaffordable => previewUnaffordableMax,
            _ => spentBrightness
        };
    }

    private void GetPreviewRange(out float min, out float max)
    {
        if (_currentState == OrbState.PreviewUnaffordable)
        {
            min = previewUnaffordableMin;
            max = previewUnaffordableMax;
        }
        else
        {
            min = previewAffordableMin;
            max = previewAffordableMax;
        }
    }

    private void AnimateBrightness(float target)
    {
        _brightnessTween = canvasGroup
            .DOFade(target, transitionDuration)
            .SetEase(brightnessEase);

        if (orbLight)
        {
            float targetIntensity = _baseLightIntensity * target;
            _lightTween = DOTween.To(
                () => orbLight.intensity,
                x => orbLight.intensity = x,
                targetIntensity,
                transitionDuration
            ).SetEase(brightnessEase);
        }
    }

    private void StartBreathing()
    {
        GetPreviewRange(out float minB, out float maxB);

        float cycle = 1f / Mathf.Max(0.001f, breathingSpeed);

        // Start bright
        canvasGroup.alpha = maxB;
        if (orbLight) orbLight.intensity = _baseLightIntensity * maxB;
        transform.localScale = Vector3.one;

        // SCALE breathing is the driver
        _scaleTween = transform
            .DOScale(Vector3.one * breathingScaleMax, cycle * 0.5f)
            .SetEase(breathingEase)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                // t = 0 (min) → 1 (max)
                float t = Mathf.InverseLerp(1f, breathingScaleMax, transform.localScale.x);

                // Interpolate brightness based on scale
                float brightness = Mathf.Lerp(minB, maxB, t);

                canvasGroup.alpha = brightness;

                if (orbLight)
                    orbLight.intensity = _baseLightIntensity * brightness;
            });
    }


    private void RestoreScale()
    {
        transform.DOScale(Vector3.one, transitionDuration)
            .SetEase(brightnessEase);
    }

    private void KillAllTweens()
    {
        _brightnessTween?.Kill();
        _breathingTween?.Kill();
        _scaleTween?.Kill();
        _lightTween?.Kill();
        _lightBreathingTween?.Kill();
    }

    private void OnDestroy() => KillAllTweens();
}
