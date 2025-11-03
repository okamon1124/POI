using UnityEngine;

public enum CardType { Creature, Object, Equipment, Environment, Spell }

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int manaCost;
    public int attack;
    public int health;
    public int speed;
}