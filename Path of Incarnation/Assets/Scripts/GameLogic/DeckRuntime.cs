using System.Collections.Generic;

public class DeckRuntime
{
    private readonly List<CardInstance> cards = new List<CardInstance>();
    private int nextId = 0;
    private System.Random rng = new System.Random();

    public IReadOnlyList<CardInstance> Cards => cards;
    public int Count => cards.Count;

    public DeckRuntime(DeckAsset deckAsset)
    {
        // Create CardInstance objects from the DeckAsset
        foreach (var entry in deckAsset.cards)
        {
            if (entry.card == null || entry.count <= 0)
                continue;

            for (int i = 0; i < entry.count; i++)
            {
                var instance = new CardInstance(entry.card, nextId++);
                instance.CurrentZone = LogicZone.Deck;
                cards.Add(instance);
            }
        }
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    public CardInstance Draw()
    {
        if (cards.Count == 0)
            return null;

        int lastIndex = cards.Count - 1;
        CardInstance top = cards[lastIndex];
        cards.RemoveAt(lastIndex);

        top.CurrentZone = LogicZone.Hand; // or set this outside if you prefer
        return top;
    }
}