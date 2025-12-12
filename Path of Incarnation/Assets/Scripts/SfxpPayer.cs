using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays sound effects in response to UiEvents.
/// 
/// Pure presentation layer - knows nothing about Board or UiRegistry.
/// Subscribes to EventBus for UiEvents and plays appropriate AudioClips.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    [Header("Card Interaction")]
    [SerializeField] private AudioClip cardHover;
    [SerializeField] private AudioClip cardGrab;
    [SerializeField] private AudioClip cardDrawn;

    [Header("Slot Interaction")]
    [SerializeField] private AudioClip slotHot;

    [Header("Combat")]
    [SerializeField] private AudioClip attackImpact;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

    [Header("Cooldown Settings")]
    [SerializeField] private float hoverCooldown = 0.1f;

    [Header("Draw Settings")]
    [SerializeField] private float drawPitchMin = 0.95f;
    [SerializeField] private float drawPitchMax = 1.05f;

    private AudioSource audioSource;
    private Dictionary<AudioClip, float> lastPlayTimes = new();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CardHoverEnterEvent>(OnCardHoverEnter);
        EventBus.Subscribe<CardGrabbedEvent>(OnCardGrabbed);
        EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
        EventBus.Subscribe<SlotHighlightHotEvent>(OnSlotHot);
        EventBus.Subscribe<CombatImpactEvent>(OnCombatImpact);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CardHoverEnterEvent>(OnCardHoverEnter);
        EventBus.Unsubscribe<CardGrabbedEvent>(OnCardGrabbed);
        EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
        EventBus.Unsubscribe<SlotHighlightHotEvent>(OnSlotHot);
        EventBus.Unsubscribe<CombatImpactEvent>(OnCombatImpact);
    }

    // -------------------- Event Handlers --------------------

    private void OnCardHoverEnter(CardHoverEnterEvent e)
    {
        PlayWithCooldown(cardHover, hoverVolume, hoverCooldown);
    }

    private void OnCardGrabbed(CardGrabbedEvent e)
    {
        Play(cardGrab);
    }

    private void OnCardDrawn(CardDrawnEvent e)
    {
        PlayWithPitchVariation(cardDrawn, defaultVolume, drawPitchMin, drawPitchMax);
    }

    private void OnSlotHot(SlotHighlightHotEvent e)
    {
        PlayWithCooldown(slotHot, hoverVolume, hoverCooldown);
    }

    private void OnCombatImpact(CombatImpactEvent e)
    {
        Play(attackImpact);
    }

    // -------------------- Play Methods --------------------

    private void Play(AudioClip clip)
    {
        Play(clip, defaultVolume);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Play a clip only if enough time has passed since last play.
    /// Useful for rapid-fire events like hovering over multiple cards.
    /// </summary>
    private void PlayWithCooldown(AudioClip clip, float volume, float cooldown)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        float now = Time.time;

        if (lastPlayTimes.TryGetValue(clip, out float lastTime))
        {
            if (now - lastTime < cooldown)
                return;
        }

        lastPlayTimes[clip] = now;
        audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Play a clip with random pitch variation.
    /// Useful for repeated sounds like drawing multiple cards.
    /// </summary>
    private void PlayWithPitchVariation(AudioClip clip, float volume, float minPitch, float maxPitch)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
        audioSource.pitch = 1f;
    }
}