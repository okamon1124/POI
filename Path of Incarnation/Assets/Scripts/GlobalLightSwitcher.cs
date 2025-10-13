using UnityEngine;

using UnityEngine.Rendering.Universal;

public class GlobalLightSwitcher : MonoBehaviour
{
    [SerializeField] UIDraggable uIDraggable;

    [SerializeField] Light2D light2d;

    [SerializeField] float turnUpLightIntensity = 1.0f;
    [SerializeField] float turnDownLightIntensity = 0f;

    private void OnEnable()
    {
        uIDraggable.dragStarted += TurnDownLight;
        uIDraggable.dragEnded += TurnUpLight;
    }

    private void OnDisable()
    {
        uIDraggable.dragStarted -= TurnDownLight;
        uIDraggable.dragEnded -= TurnUpLight;
    }

    private void TurnUpLight()
    {
        light2d.intensity = turnUpLightIntensity;
    }

    private void TurnDownLight()
    {
        light2d.intensity = turnDownLightIntensity;
    }
}
