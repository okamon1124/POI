using DG.Tweening;
using UnityEngine;

/// <summary>
/// Handles all animations for UiCard (attack, movement, scaling).
/// Separated from UiCard to keep animation logic isolated and reusable.
/// </summary>
public class UiCardAnimator
{
    private readonly RectTransform rectTransform;
    private readonly RectTransform visual;
    private readonly UiCardConfig config;

    private Tween scaleTween;
    private Tween moveTween;

    public UiCardAnimator(RectTransform rectTransform, RectTransform visual, UiCardConfig config)
    {
        this.rectTransform = rectTransform;
        this.visual = visual;
        this.config = config;
    }

    #region Scale Animations

    public void PlayScaleTween(Vector3 targetScale)
    {
        scaleTween?.Kill();
        scaleTween = rectTransform
            .DOScale(targetScale, config.tweenDuration)
            .SetEase(config.ease);
    }

    public void SetScaleImmediate(Vector3 scale)
    {
        scaleTween?.Kill();
        rectTransform.localScale = scale;
    }

    #endregion

    #region Visual Lift (Hand Hover)

    public void LiftVisual(Vector3 baseLocalPos)
    {
        if (!visual) return;

        visual.DOKill(false);

        visual.DOAnchorPos(new Vector2(baseLocalPos.x, baseLocalPos.y + config.liftAmount), config.tweenDuration)
            .SetEase(config.ease)
            .SetId("hover_vis_move");
    }

    public void LowerVisual(Vector3 baseLocalPos)
    {
        if (!visual) return;

        DOTween.Kill("hover_vis_move");

        visual.DOAnchorPos(new Vector2(baseLocalPos.x, baseLocalPos.y), config.tweenDuration)
            .SetEase(config.ease)
            .SetId("hover_vis_move");
    }

    public void SetVisualPositionImmediate(Vector3 localPos)
    {
        if (!visual) return;

        DOTween.Kill("hover_vis_move");
        visual.localPosition = localPos;
    }

    #endregion

    #region Movement

    public void AnimateToPosition(Vector3 worldPosition, System.Action onComplete = null)
    {
        moveTween?.Kill();
        moveTween = rectTransform
            .DOMove(worldPosition, config.tweenDuration)
            .SetEase(config.ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void SetPositionImmediate(Vector3 worldPosition)
    {
        moveTween?.Kill();
        rectTransform.position = worldPosition;
    }

    #endregion

    #region Attack Animation

    /// <summary>
    /// Plays windup ¡÷ attack ¡÷ recover animation sequence.
    /// Direction: 1 = right (player), -1 = left (opponent)
    /// </summary>
    public void PlayAttackImpact(float direction)
    {
        if (rectTransform == null)
        {
            Debug.LogError("[UiCardAnimator] rectTransform is null, cannot play attack impact.");
            return;
        }

        Vector3 startPos = rectTransform.position;
        Vector3 startEuler = rectTransform.eulerAngles;

        rectTransform.DOKill();

        // Calculate positions
        Vector3 windupPos = startPos + new Vector3(direction * config.windupDistance, 0f, 0f);
        Vector3 attackPos = startPos + new Vector3(-direction * config.attackDistance, 0f, 0f);

        // Calculate rotations
        Vector3 windupRot = startEuler + new Vector3(0f, 0f, -direction * config.windupAngle);
        Vector3 attackRot = startEuler + new Vector3(0f, 0f, direction * config.attackAngle);

        Sequence seq = DOTween.Sequence();

        // Phase 1: Windup (pull back)
        seq.Append(rectTransform.DOMove(windupPos, config.windupTime).SetEase(Ease.OutQuad));
        seq.Join(rectTransform.DORotate(windupRot, config.windupTime).SetEase(Ease.OutQuad));

        // Phase 2: Attack (lunge forward)
        seq.Append(rectTransform.DOMove(attackPos, config.attackTime).SetEase(Ease.InQuad));
        seq.Join(rectTransform.DORotate(attackRot, config.attackTime * 0.7f).SetEase(Ease.OutQuad));

        // Phase 3: Recover (return to original)
        seq.Append(rectTransform.DOMove(startPos, config.recoverTime).SetEase(Ease.OutQuad));
        seq.Join(rectTransform.DORotate(startEuler, config.recoverTime).SetEase(Ease.OutQuad));
    }

    #endregion

    #region Dragging Visuals

    public void StartDragging()
    {
        rectTransform.DOScale(Vector3.one * config.draggingScale, config.tweenDuration * 0.5f)
            .SetEase(config.ease);
    }

    #endregion

    #region Cleanup

    public void KillAllTweens()
    {
        scaleTween?.Kill();
        moveTween?.Kill();
        rectTransform?.DOKill();
        visual?.DOKill();
        DOTween.Kill("hover_vis_move");
    }

    #endregion
}