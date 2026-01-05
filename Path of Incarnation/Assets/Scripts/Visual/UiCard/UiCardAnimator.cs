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

    /// <summary>
    /// Play attack impact animation - card winds up, lunges forward, then returns.
    /// Fires CombatImpactEvent at the exact moment of impact.
    /// </summary>
    /// <param name="direction">1 for player (right), -1 for opponent (left)</param>
    /// <param name="isPlayer">Used for the CombatImpactEvent</param>
    public void PlayAttackImpact(float direction, bool isPlayer = true)
    {
        if (!rectTransform) return;

        // Store original state
        Vector3 originalPos = rectTransform.anchoredPosition;

        // Calculate offsets based on direction (horizontal - X axis)
        // Player attacks left (direction = 1), opponent attacks right (direction = -1)
        Vector3 windupOffset = new Vector3(config.windupDistance * 100f * direction, 0f, 0f);
        Vector3 attackOffset = new Vector3(-config.attackDistance * 100f * direction, 0f, 0f);

        float windupRotation = -config.windupAngle * direction;
        float attackRotation = config.attackAngle * direction;

        // Kill any existing attack tweens
        DOTween.Kill(rectTransform, "attack");

        // Create attack sequence: windup -> attack -> recover
        Sequence attackSequence = DOTween.Sequence();

        // Phase 1: Windup - pull back and rotate
        attackSequence.Append(rectTransform
            .DOAnchorPos(originalPos + windupOffset, config.windupTime)
            .SetEase(Ease.OutQuad));
        attackSequence.Join(rectTransform
            .DOLocalRotate(new Vector3(0f, 0f, windupRotation), config.windupTime)
            .SetEase(Ease.OutQuad));

        // Phase 2: Attack - lunge forward with rotation
        attackSequence.Append(rectTransform
            .DOAnchorPos(originalPos + attackOffset, config.attackTime)
            .SetEase(Ease.OutQuad));
        attackSequence.Join(rectTransform
            .DOLocalRotate(new Vector3(0f, 0f, attackRotation), config.attackTime)
            .SetEase(Ease.OutQuad));

        // Fire impact event at the exact moment attack phase completes
        attackSequence.AppendCallback(() =>
        {
            EventBus.Publish(new CombatImpactEvent(isPlayer));
        });

        // Phase 3: Recover - return to original position and rotation
        attackSequence.Append(rectTransform
            .DOAnchorPos(originalPos, config.recoverTime)
            .SetEase(Ease.OutBack));
        attackSequence.Join(rectTransform
            .DOLocalRotate(Vector3.zero, config.recoverTime)
            .SetEase(Ease.OutBack));

        attackSequence.SetId("attack");
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
        DOTween.Kill("attack");
    }
}