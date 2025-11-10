using UnityEngine;

public enum CardType { Creature, Object, Equipment, Environment, Spell }

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int manaCost, attack, health, speed;

    public PlayRule[] playerMoveRules;
}