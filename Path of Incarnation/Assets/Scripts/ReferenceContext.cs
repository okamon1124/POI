using UnityEngine;

[DisallowMultipleComponent]
public class ReferenceContext : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform fieldParent;   // e.g., "Field"
    [SerializeField] private CardGrid cardGrid;       // your grid object

    public Transform FieldParent => fieldParent;
    public CardGrid Grid => cardGrid;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!GetComponent<Canvas>())
            Debug.LogWarning($"{name}: ReferenceContext is intended to live on a Canvas.");
    }
#endif
}