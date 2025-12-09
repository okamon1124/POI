using DG.Tweening;
using UnityEngine;

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

    // ---------------------------- Scale ----------------------------
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

    // ---------------------------- Hover Lift ----------------------------
    public void LiftVisual(Vector3 baseLocalPos)
    {
        if (!visual) return;
        visual.DOKill(false);

        visual.DOAnchorPos(new Vector2(baseLocalPos.x, baseLocalPos.y + config.liftAmount),
                           config.tweenDuration)
              .SetEase(config.ease)
              .SetId("hover_vis_move");
    }

    public void LowerVisual(Vector3 baseLocalPos)
    {
        if (!visual) return;
        DOTween.Kill("hover_vis_move");

        visual.DOAnchorPos(new Vector2(baseLocalPos.x, baseLocalPos.y),
                           config.tweenDuration)
              .SetEase(config.ease)
              .SetId("hover_vis_move");
    }

    // ---------------------------- Movement ----------------------------
    public void AnimateToPosition(Vector3 worldPosition, System.Action onComplete = null)
    {
        moveTween?.Kill();
        moveTween = rectTransform
            .DOMove(worldPosition, config.tweenDuration)
            .SetEase(config.ease)
            .OnComplete(() =>
            {
                // Notify systems that this card finished moving
                var card = rectTransform.GetComponent<UiCard>();
                if (card != null)
                {
                    EventBus.Publish(new UiCardSettledEvent(card));
                }

                onComplete?.Invoke();
            });
    }

    public void SetPositionImmediate(Vector3 worldPosition)
    {
        moveTween?.Kill();
        rectTransform.position = worldPosition;
    }

    // ---------------------------- Attack ----------------------------
    public void PlayAttackImpact(float direction)
    {
        // unchanged...
    }

    // ---------------------------- Drag ----------------------------
    public void StartDragging()
    {
        rectTransform
            .DOScale(Vector3.one * config.draggingScale, config.tweenDuration * 0.5f)
            .SetEase(config.ease);
    }

    // ---------------------------- Cleanup ----------------------------
    public void KillAllTweens()
    {
        scaleTween?.Kill();
        moveTween?.Kill();
        rectTransform?.DOKill();
        visual?.DOKill();
        DOTween.Kill("hover_vis_move");
    }
}
