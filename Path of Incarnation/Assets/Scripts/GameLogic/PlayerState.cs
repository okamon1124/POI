using System.Collections.Generic;

public class PlayerState
{
    public DeckRuntime Deck { get; private set; }

    public List<CardInstance> Hand { get; } = new List<CardInstance>();
    public List<CardInstance> Main { get; } = new List<CardInstance>();
    public List<CardInstance> Deployment { get; } = new List<CardInstance>();
    public List<CardInstance> Advance { get; } = new List<CardInstance>();
    public List<CardInstance> Combat { get; } = new List<CardInstance>();
    public List<CardInstance> Graveyard { get; } = new List<CardInstance>();

    public PlayerState(DeckAsset deckAsset)
    {
        Deck = new DeckRuntime(deckAsset);
        Deck.Shuffle();
    }

    // ---- Drawing ----
    public CardInstance DrawCard()
    {
        var card = Deck.Draw();
        if (card == null)
            return null;

        MoveCardToZone(card, LogicZone.Hand);
        return card;
    }

    // ---- Generic movement between logic zones ----
    public void MoveCardToZone(CardInstance card, LogicZone targetZone)
    {
        if (card == null)
            return;

        // Remove from any zone list it might be in
        Hand.Remove(card);
        Main.Remove(card);
        Deployment.Remove(card);
        Advance.Remove(card);
        Combat.Remove(card);
        Graveyard.Remove(card);
        // (Deck is handled by DeckRuntime, so no list here)

        card.CurrentZone = targetZone;

        switch (targetZone)
        {
            case LogicZone.Hand:
                Hand.Add(card);
                break;
            case LogicZone.Main:
                Main.Add(card);
                break;
            case LogicZone.Deployment:
                Deployment.Add(card);
                break;
            case LogicZone.Advance:
                Advance.Add(card);
                break;
            case LogicZone.Combat:
                Combat.Add(card);
                break;
            case LogicZone.Graveyard:
                Graveyard.Add(card);
                break;
            case LogicZone.Deck:
                // usually you don't move *into* deck through this method;
                // DeckRuntime manages its own list.
                break;
        }
    }

    // Convenience: play from hand into a specific zone
    public bool PlayFromHandTo(CardInstance card, LogicZone targetZone)
    {
        if (card == null || !Hand.Contains(card))
            return false;

        MoveCardToZone(card, targetZone);
        return true;
    }
}