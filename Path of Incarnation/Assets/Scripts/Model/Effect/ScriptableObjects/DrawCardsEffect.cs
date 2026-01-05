using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SubEffect that draws cards from the owner's deck to their hand.
/// 
/// Example usage:
/// - "Draw 2 cards" ¡÷ amount = 2
/// - "Draw 1 card" ¡÷ amount = 1
/// 
/// Writes drawn cards to whiteboard if writeToKey is set,
/// allowing follow-up effects like "Draw 2, discard 1 of them".
/// </summary>
[CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Effects/SubEffects/Draw Cards")]
public class DrawCardsEffect : SubEffect
{
    [Header("Settings")]
    [SerializeField] private FixedValue amount = new(1);

    [Tooltip("If true, draws for the opponent instead of the caster.")]
    [SerializeField] private bool targetOpponent = false;

    public override async UniTask<bool> Execute(EffectContext context)
    {
        int cardsToDraw = amount.Evaluate(context);

        if (cardsToDraw <= 0)
        {
            Debug.LogWarning("[DrawCardsEffect] Amount is 0 or negative, skipping.");
            return true; // Not a failure, just nothing to do
        }

        // Determine who draws
        Owner drawingOwner = targetOpponent ? context.GetOpponentOwner() : context.Owner;
        Deck deck = context.GetDeck(drawingOwner);

        if (deck == null)
        {
            Debug.LogWarning($"[DrawCardsEffect] No deck available for {drawingOwner}.");
            return false;
        }

        var drawnCards = new List<CardInstance>();

        for (int i = 0; i < cardsToDraw; i++)
        {
            // Try to draw from deck
            if (!deck.TryDraw(out CardData cardData))
            {
                Debug.LogWarning($"[DrawCardsEffect] Deck is empty, could not draw card {i + 1}/{cardsToDraw}.");
                break;
            }

            // Spawn the card to hand
            bool success = context.Board.TrySpawnCardToZone(
                cardData,
                ZoneType.Hand,
                drawingOwner,
                out CardInstance drawnCard,
                out string reason
            );

            if (success && drawnCard != null)
            {
                drawnCards.Add(drawnCard);
                Debug.Log($"[DrawCardsEffect] {drawingOwner} drew: {cardData.cardName}");

                // TODO: Wait for draw animation if needed
                await UniTask.Delay(200);
            }
            else
            {
                Debug.LogWarning($"[DrawCardsEffect] Failed to spawn card to hand: {reason}");
                // Card was drawn from deck but couldn't be placed - you might want to handle this
                // (e.g., discard it, or put it back)
            }
        }

        // Store drawn cards in whiteboard for potential follow-up effects
        if (drawnCards.Count > 0)
        {
            StoreResult(context, drawnCards);
        }

        // Return true if we drew at least one card
        return drawnCards.Count > 0;
    }

    public override bool CanExecute(EffectContext context)
    {
        if (!base.CanExecute(context))
            return false;

        // Check if the appropriate deck has cards
        Owner drawingOwner = targetOpponent ? context.GetOpponentOwner() : context.Owner;
        Deck deck = context.GetDeck(drawingOwner);

        if (deck == null || deck.Count == 0)
            return false;

        return true;
    }

    public override string GetDescription()
    {
        string target = targetOpponent ? "opponent draws" : "Draw";
        int value = amount.RawValue;
        string plural = value == 1 ? "card" : "cards";

        return $"{target} {value} {plural}";
    }
}