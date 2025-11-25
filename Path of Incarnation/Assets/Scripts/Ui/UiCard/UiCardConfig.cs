using DG.Tweening;
using UnityEngine;

/// <summary>
/// Configuration for UiCard animation and visual behavior.
/// Create via Assets > Create > Card System > UiCard Config
/// </summary>
[CreateAssetMenu(fileName = "UiCardConfig", menuName = "Card System/UiCard Config")]
public class UiCardConfig : ScriptableObject
{
    [Header("Hover Animation")]
    [Tooltip("Scale multiplier when hovering over card")]
    [Range(1f, 2f)]
    public float hoverScale = 1.1f;

    [Tooltip("Duration for scale/move tweens")]
    public float tweenDuration = 0.2f;

    [Tooltip("Easing function for animations")]
    public Ease ease = Ease.OutQuad;

    [Header("Zone Scales")]
    [Tooltip("Base scale when card is in hand")]
    public float handBaseScale = 1.4f;

    [Tooltip("Base scale when card is on board")]
    public float cellBaseScale = 1.1f;

    [Tooltip("Scale when dragging card")]
    public float draggingScale = 1.1f;

    [Header("Hand Visual Effects")]
    [Tooltip("How much to lift card vertically when hovering in hand")]
    public float liftAmount = 30f;

    [Header("Availability Light")]
    [Tooltip("Light intensity when card is playable")]
    public float availableLightIntensity = 1.5f;

    [Tooltip("Light intensity when card is not playable")]
    public float unavailableLightIntensity = 0f;

    [Header("Attack Animation")]
    [Tooltip("Distance to pull back during windup")]
    public float windupDistance = 0.2f;

    [Tooltip("Distance to lunge forward during attack")]
    public float attackDistance = 0.5f;

    [Tooltip("Duration of windup phase")]
    public float windupTime = 0.08f;

    [Tooltip("Duration of attack phase")]
    public float attackTime = 0.12f;

    [Tooltip("Duration of recovery phase")]
    public float recoverTime = 0.15f;

    [Tooltip("Rotation angle during windup (degrees)")]
    public float windupAngle = 6f;

    [Tooltip("Rotation angle during attack (degrees)")]
    public float attackAngle = 18f;
}