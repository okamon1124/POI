using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card")]
public class CardData : ScriptableObject
{
    public Sprite cardSprite;
    public string cardName;
    public CardType cardType;
    public int manaCost;

    [Header("Creature Stats")]
    public int power;
    public int health;
    public int speed;

    public PlayRule[] playerMoveRules;
    public CombatRule[] combatRules;
}