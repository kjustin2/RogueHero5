using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    [CreateAssetMenu(menuName = "Rogue Hero 5/Ability Presentation", fileName = "AbilityPresentation")]
    public sealed class AbilityPresentationDefinition : ScriptableObject
    {
        [SerializeField] private MoveSlot slot;
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private Sprite icon;
        [SerializeField] private Color primaryColor = Color.white;
        [SerializeField] private Color secondaryColor = Color.white;
        [SerializeField] private AudioClip castSound;
        [SerializeField] private AudioClip impactSound;
        [SerializeField] private float castFlashRadius = 0.8f;
        [SerializeField] private float impactFlashRadius = 1.2f;
        [SerializeField] private float trailSeconds = 0.15f;
        [SerializeField] private float hitStopSeconds = 0.04f;
        [SerializeField] private float cameraShake = 0.08f;
        [SerializeField] private float fovKick = 1f;

        public MoveSlot Slot => slot;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public AudioClip CastSound => castSound;
        public AudioClip ImpactSound => impactSound;
        public float CastFlashRadius => castFlashRadius;
        public float ImpactFlashRadius => impactFlashRadius;
        public float TrailSeconds => trailSeconds;
        public float HitStopSeconds => hitStopSeconds;
        public float CameraShake => cameraShake;
        public float FovKick => fovKick;

        public void Configure(
            MoveSlot newSlot,
            string newDisplayName,
            Sprite newIcon,
            Color newPrimaryColor,
            Color newSecondaryColor,
            AudioClip newCastSound,
            AudioClip newImpactSound,
            float newCastFlashRadius,
            float newImpactFlashRadius,
            float newTrailSeconds,
            float newHitStopSeconds,
            float newCameraShake,
            float newFovKick)
        {
            slot = newSlot;
            displayName = newDisplayName;
            icon = newIcon;
            primaryColor = newPrimaryColor;
            secondaryColor = newSecondaryColor;
            castSound = newCastSound;
            impactSound = newImpactSound;
            castFlashRadius = newCastFlashRadius;
            impactFlashRadius = newImpactFlashRadius;
            trailSeconds = newTrailSeconds;
            hitStopSeconds = newHitStopSeconds;
            cameraShake = newCameraShake;
            fovKick = newFovKick;
        }

        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new List<string>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                issues.Add("Display name is required.");
            }

            if (castFlashRadius < 0f || impactFlashRadius < 0f)
            {
                issues.Add("Flash radii cannot be negative.");
            }

            if (trailSeconds < 0f || hitStopSeconds < 0f || cameraShake < 0f)
            {
                issues.Add("Feedback timings and intensity cannot be negative.");
            }

            return issues;
        }
    }
}
