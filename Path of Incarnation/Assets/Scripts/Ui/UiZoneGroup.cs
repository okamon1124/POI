using UnityEngine;

public class UiZoneGroup : MonoBehaviour
{
    [Header("Logical Zone Info")]
    public ZoneType zoneType;
    public Owner owner;

    [Header("Slots in this zone (UI)")]
    [SerializeField] private UiSlot[] slots;
    public UiSlot[] Slots => slots;

    [Header("Helpers")]
    [SerializeField] private bool autoCollectChildren = true;

    private void Reset()
    {
        if (autoCollectChildren)
            slots = GetComponentsInChildren<UiSlot>(includeInactive: true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoCollectChildren)
            slots = GetComponentsInChildren<UiSlot>(includeInactive: true);
    }
#endif
}