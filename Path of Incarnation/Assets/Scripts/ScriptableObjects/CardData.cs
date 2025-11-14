using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card")]
public class CardData : ScriptableObject
{
    public Sprite cardSprite;
    public string cardName;
    public CardType cardType;
    public int manaCost, power, health, speed;

    public PlayRule[] playerMoveRules;
}