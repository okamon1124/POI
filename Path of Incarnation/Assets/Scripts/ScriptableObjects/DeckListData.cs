using UnityEngine;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Card Game/Deck")]
public class DeckListData : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public CardData card;
        public int count;
    }

    public Entry[] entries;
}