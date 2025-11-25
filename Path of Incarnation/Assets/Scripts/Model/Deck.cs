using System;
using System.Collections.Generic;

public class Deck
{
    private readonly List<CardData> _cards = new();
    private readonly Random _rng;

    public int Count => _cards.Count;

    public Deck(DeckListData data, int? seed = null)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        foreach (var e in data.entries)
        {
            if (e.card == null || e.count <= 0) continue;

            for (int i = 0; i < e.count; i++)
                _cards.Add(e.card);
        }

        _rng = seed.HasValue ? new Random(seed.Value) : new Random();

        Shuffle();
    }

    public void Shuffle()
    {
        // Fisher¡VYates
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public bool TryDraw(out CardData card)
    {
        if (_cards.Count == 0)
        {
            card = null;
            return false;
        }

        int lastIndex = _cards.Count - 1;
        card = _cards[lastIndex];
        _cards.RemoveAt(lastIndex);
        return true;
    }
}