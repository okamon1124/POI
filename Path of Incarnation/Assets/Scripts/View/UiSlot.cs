using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Rendering.Universal;

public enum SlotHighlightLevel { Off, Available, Hot }

[RequireComponent(typeof(RectTransform))]
public class UiSlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDropHandler
{
    public Slot ModelSlot { get; private set; }
    public UiSlot NextUiSlot;

    public RectTransform RectTransform { get; private set; }

    private readonly List<UiCard> _occupants = new();
    public IReadOnlyList<UiCard> Occupants => _occupants;
    public event Action<UiSlot> OccupantsChanged;

    [Header("Highlight")]
    [SerializeField] private Light2D highlightLight;
    [SerializeField, Range(0, 10)] private float offIntensity = 0f;
    [SerializeField, Range(0, 10)] private float availableIntensity = 1.2f;
    [SerializeField, Range(0, 10)] private float hotIntensity = 2.0f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private Tween _tween;
    private SlotHighlightLevel currentHighlight = SlotHighlightLevel.Off;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
        ApplyHighlight(currentHighlight);
    }

    public void Bind(Slot slot)
    {
        ModelSlot = slot;
    }

    // ---- Occupants API ----
    public void AttachCard(UiCard card)
    {
        if (card == null) return;
        if (_occupants.Contains(card)) return;

        _occupants.Add(card);
        OccupantsChanged?.Invoke(this);
    }

    public void DetachCard(UiCard card)
    {
        if (card == null) return;
        if (_occupants.Remove(card))
            OccupantsChanged?.Invoke(this);
    }

    // ---- Highlight API ----
    public void SetHighlightLevel(SlotHighlightLevel level)
    {
        if (currentHighlight == level) return;
        currentHighlight = level;

        ApplyTweenHighlight(level);
    }

    private void ApplyHighlight(SlotHighlightLevel level)
    {
        if (!highlightLight) return;

        highlightLight.intensity = level switch
        {
            SlotHighlightLevel.Off => offIntensity,
            SlotHighlightLevel.Available => availableIntensity,
            SlotHighlightLevel.Hot => hotIntensity,
            _ => offIntensity
        };
    }

    private void ApplyTweenHighlight(SlotHighlightLevel level)
    {
        if (!highlightLight) return;

        float target = level switch
        {
            SlotHighlightLevel.Off => offIntensity,
            SlotHighlightLevel.Available => availableIntensity,
            SlotHighlightLevel.Hot => hotIntensity,
            _ => offIntensity
        };

        _tween?.Kill();
        _tween = DOTween.To(
            () => highlightLight.intensity,
            v => highlightLight.intensity = v,
            target,
            fadeDuration
        ).SetEase(ease);
    }

    // ---- Pointer + Drop ----
    public void OnPointerEnter(PointerEventData e)
    {
        EventBus.Publish(new SlotPointerEnterEvent(this));
    }

    public void OnPointerExit(PointerEventData e)
    {
        EventBus.Publish(new SlotPointerExitEvent(this));
    }

    public void OnDrop(PointerEventData e)
    {
        var uiCard = e.pointerDrag ? e.pointerDrag.GetComponent<UiCard>() : null;
        if (!uiCard) return;

        EventBus.Publish(new SlotDropEvent(this, uiCard));
    }
}
