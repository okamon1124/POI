using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.Universal;

public class GlobalLightResponder : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField, Range(0, 1)] private float normal = 1f;
    [SerializeField, Range(0, 1)] private float dimmed = 0.4f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private void OnEnable() { EventBus.Subscribe<PlayerDragChangedEvent>(OnDrag); }
    private void OnDisable() { EventBus.Unsubscribe<PlayerDragChangedEvent>(OnDrag); }

    private void OnDrag(PlayerDragChangedEvent e)
    {
        if (!globalLight) return;
        float target = (e.Dragged != null) ? dimmed : normal;
        DOTween.Kill(globalLight);
        DOTween.To(() => globalLight.intensity, v => globalLight.intensity = v, target, duration).SetEase(ease);
    }
}