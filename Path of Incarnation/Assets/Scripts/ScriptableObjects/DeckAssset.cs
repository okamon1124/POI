using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Card Game/Deck Asset")]
public class DeckAsset : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public CardData card;
        public int count = 1;
    }

    public List<Entry> cards = new();
}