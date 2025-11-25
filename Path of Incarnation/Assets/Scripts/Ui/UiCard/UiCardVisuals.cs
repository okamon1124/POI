using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Manages visual elements and updates for UiCard.
/// Handles sprite/text updates, lights, and visual state changes.
/// </summary>
public class UiCardVisuals : MonoBehaviour
{
    [Header("Visual Refs")]
    [SerializeField] private Image artImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text speedText;

    [Header("Lights")]
    [SerializeField] private Light2D availabilityLight;
    [SerializeField] private Light2D enemyLight;

    [Header("Visual Transform")]
    [SerializeField] private RectTransform visual;

    private UiCardConfig config;

    public RectTransform Visual => visual;

    public void Initialize(UiCardConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// Updates all visual elements based on card instance data.
    /// </summary>
    public void RefreshFromCardInstance(CardInstance cardInstance)
    {
        if (cardInstance == null) return;

        var data = cardInstance.Data;

        // Update sprites and text
        if (artImage) artImage.sprite = data.cardSprite;
        if (nameText) nameText.text = data.cardName;
        if (costText) costText.text = data.manaCost.ToString();

        // Update dynamic stats
        if (powerText) powerText.text = cardInstance.CurrentPower.ToString();
        if (healthText) healthText.text = cardInstance.CurrentHealth.ToString();
        if (speedText) speedText.text = cardInstance.CurrentSpeed.ToString();

        // Update owner-specific visuals
        UpdateOwnerLight(cardInstance.Owner);
    }

    /// <summary>
    /// Updates the availability light based on interactable state.
    /// </summary>
    public void SetAvailabilityLight(bool isAvailable)
    {
        if (!availabilityLight || config == null) return;

        availabilityLight.intensity = isAvailable
            ? config.availableLightIntensity
            : config.unavailableLightIntensity;
    }

    /// <summary>
    /// Shows/hides enemy light based on card owner.
    /// </summary>
    private void UpdateOwnerLight(Owner owner)
    {
        if (enemyLight != null)
        {
            enemyLight.enabled = (owner == Owner.Opponent);
        }
    }

    /// <summary>
    /// Updates a specific stat text (useful for reactive updates).
    /// </summary>
    public void UpdateStat(StatType statType, int value)
    {
        switch (statType)
        {
            case StatType.Power:
                if (powerText) powerText.text = value.ToString();
                break;
            case StatType.Health:
                if (healthText) healthText.text = value.ToString();
                break;
            case StatType.Speed:
                if (speedText) speedText.text = value.ToString();
                break;
        }
    }

    public enum StatType
    {
        Power,
        Health,
        Speed
    }
}