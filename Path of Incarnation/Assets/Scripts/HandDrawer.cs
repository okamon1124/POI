using UnityEngine;
using NaughtyAttributes;

public class HandDrawer : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Zone handZone;                  // logical hand zone
    [SerializeField] private RectTransform cardsInHandRoot;  // your visual "CardsInHand" container
    [SerializeField] private RectTransform spawnPoint;       // where cards appear
    [SerializeField] private UiCard cardPrefab;

    [Header("Limits")]
    [SerializeField] private int maxHandSize = 10;

    [Button]
    public void DrawCard()
    {
        if (!handZone || !spawnPoint || !cardPrefab || !cardsInHandRoot)
        {
            Debug.LogWarning("Drawer refs missing."); return;
        }
        if (handZone.Occupants.Count >= maxHandSize) return;

        // 1) Instantiate under CardsInHand so it renders above the board
        UiCard card = Instantiate(cardPrefab, cardsInHandRoot);

        // 2) Place the card so its visual center lands at spawnPoint center (pivot correction)
        var rt = card.GetComponent<RectTransform>();
        Vector3 spawnWorld = spawnPoint.TransformPoint(spawnPoint.rect.center);

        Vector2 size = rt.rect.size;
        Vector2 localCenter2D = (new Vector2(0.5f, 0.5f) - rt.pivot) * size;
        Vector3 worldOffset = rt.TransformVector(new Vector3(localCenter2D.x, localCenter2D.y, 0f));
        rt.position = spawnWorld - worldOffset;

        // 3) Register card to logical hand; this fires OccupantsChanged -> HandSplineLayout.Reflow()
        card.AssignZone(handZone);
        handZone.TryAdd(card);
    }
}