using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardVisualSetter : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text manaCostText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text speedText;

    public void SetCardVisualFromData(CardData data)
    {
        cardImage.sprite = data.cardSprite;
        cardNameText.text = data.cardName;
        manaCostText.text = data.manaCost.ToString();
        powerText.text = data.power.ToString();
        speedText.text = data.speed.ToString();
    }
}