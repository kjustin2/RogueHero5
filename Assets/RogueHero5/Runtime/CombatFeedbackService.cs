using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RogueHero5
{
    public sealed class CombatFeedbackService : MonoBehaviour
    {
        [SerializeField] private AbilityPresentationDefinition[] abilityPresentations = new AbilityPresentationDefinition[0];
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private Image screenFlash;
        [SerializeField] private ThirdPersonCameraRig cameraRig;

        private readonly Dictionary<MoveSlot, AbilityPresentationDefinition> presentationsBySlot = new Dictionary<MoveSlot, AbilityPresentationDefinition>();
        private Coroutine hitStopRoutine;
        private float originalTimeScale = 1f;

        public static CombatFeedbackService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            RebuildLookup();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Time.timeScale = originalTimeScale;
        }

        public void Configure(AbilityPresentationDefinition[] presentations, AudioSource newSfxSource, Image newScreenFlash, ThirdPersonCameraRig newCameraRig)
        {
            abilityPresentations = presentations ?? new AbilityPresentationDefinition[0];
            sfxSource = newSfxSource;
            screenFlash = newScreenFlash;
            cameraRig = newCameraRig;
            RebuildLookup();
        }

        public AbilityPresentationDefinition GetPresentation(MoveSlot slot)
        {
            return presentationsBySlot.TryGetValue(slot, out AbilityPresentationDefinition presentation) ? presentation : null;
        }

        public void MoveStarted(MoveDefinition definition, Vector3 position, Vector3 direction)
        {
            AbilityPresentationDefinition presentation = GetPresentation(definition.Slot);
            Color color = presentation != null ? presentation.PrimaryColor : definition.EffectColor;
            SpawnFlash(position + Vector3.up * 0.8f, color, presentation != null ? presentation.CastFlashRadius : 0.7f, 0.18f);
            SpawnTrail(position, direction, color, presentation != null ? presentation.TrailSeconds : 0.12f);
            PlayOneShot(presentation != null ? presentation.CastSound : null, 0.65f);
            cameraRig?.AddImpulse(presentation != null ? presentation.CameraShake * 0.45f : 0.035f, presentation != null ? presentation.FovKick * 0.45f : 0.35f);
        }

        public void MoveImpacted(MoveDefinition definition, Vector3 position, bool damagedTarget)
        {
            AbilityPresentationDefinition presentation = GetPresentation(definition.Slot);
            Color color = presentation != null ? presentation.SecondaryColor : definition.EffectColor;
            SpawnFlash(position + Vector3.up * 0.55f, color, presentation != null ? presentation.ImpactFlashRadius : 1.1f, 0.22f);

            if (damagedTarget)
            {
                PlayOneShot(presentation != null ? presentation.ImpactSound : null, 0.78f);
                cameraRig?.AddImpulse(presentation != null ? presentation.CameraShake : 0.08f, presentation != null ? presentation.FovKick : 0.8f);
                RequestHitStop(presentation != null ? presentation.HitStopSeconds : 0.035f);
                FlashScreen(new Color(color.r, color.g, color.b, 0.18f), 0.12f);
            }
        }

        public void BossWarning(BossAttackDefinition attack, Vector3 position)
        {
            if (attack == null)
            {
                return;
            }

            SpawnFlash(position + Vector3.up * 0.1f, attack.TelegraphColor, Mathf.Max(0.4f, attack.Radius), 0.16f);
            PlayOneShot(attack.WarningSound, 0.65f);
        }

        public void BossImpact(BossAttackDefinition attack, Vector3 position, bool damagedTarget)
        {
            if (attack == null)
            {
                return;
            }

            SpawnFlash(position + Vector3.up * 0.25f, attack.ImpactColor, Mathf.Max(attack.Radius, 1f), 0.2f);
            if (damagedTarget)
            {
                PlayOneShot(attack.ImpactSound, 0.85f);
                cameraRig?.AddImpulse(0.13f, 1.0f);
                FlashScreen(new Color(attack.ImpactColor.r, attack.ImpactColor.g, attack.ImpactColor.b, 0.22f), 0.14f);
                RequestHitStop(0.045f);
            }
        }

        public void ActorDamaged(FighterActor actor, DamageEvent damageEvent)
        {
            if (actor == null)
            {
                return;
            }

            Color color = actor.Team == ActorTeam.Player ? new Color(1f, 0.18f, 0.12f, 1f) : new Color(0.3f, 0.8f, 1f, 1f);
            SpawnFlash(actor.transform.position + Vector3.up * 1.0f, color, actor.BodyRadius * 1.7f, 0.14f);
        }

        public void CounterTriggered(FighterActor actor, Vector3 position)
        {
            SpawnFlash(position + Vector3.up * 0.55f, new Color(1f, 0.86f, 0.25f, 1f), 2.2f, 0.24f);
            cameraRig?.AddImpulse(0.14f, 1.2f);
            FlashScreen(new Color(1f, 0.86f, 0.25f, 0.18f), 0.12f);
            RequestHitStop(0.05f);
        }

        private void RebuildLookup()
        {
            presentationsBySlot.Clear();
            for (int i = 0; i < abilityPresentations.Length; i++)
            {
                AbilityPresentationDefinition presentation = abilityPresentations[i];
                if (presentation != null)
                {
                    presentationsBySlot[presentation.Slot] = presentation;
                }
            }
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private void SpawnFlash(Vector3 position, Color color, float radius, float lifetime)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Combat Flash";
            flash.transform.position = position;
            flash.transform.localScale = Vector3.one * Mathf.Max(0.1f, radius);

            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = new Color(color.r, color.g, color.b, 0.55f);
                renderer.sharedMaterial = material;
            }

            StartCoroutine(FadeAndDestroy(flash, lifetime));
        }

        private void SpawnTrail(Vector3 position, Vector3 direction, Color color, float lifetime)
        {
            if (lifetime <= 0f)
            {
                return;
            }

            GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trail.name = "Ability Trail";
            trail.transform.position = position + Vector3.up * 0.7f - direction.normalized * 0.55f;
            trail.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward, Vector3.up);
            trail.transform.localScale = new Vector3(0.2f, 0.2f, 1.7f);

            Collider collider = trail.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = trail.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = new Color(color.r, color.g, color.b, 0.5f);
                renderer.sharedMaterial = material;
            }

            StartCoroutine(FadeAndDestroy(trail, lifetime));
        }

        private IEnumerator FadeAndDestroy(GameObject target, float lifetime)
        {
            float elapsed = 0f;
            Vector3 startScale = target.transform.localScale;
            while (target != null && elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
                target.transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, t);
                yield return null;
            }

            if (target != null)
            {
                Destroy(target);
            }
        }

        private void FlashScreen(Color color, float lifetime)
        {
            if (screenFlash == null)
            {
                return;
            }

            StartCoroutine(ScreenFlashRoutine(color, lifetime));
        }

        private IEnumerator ScreenFlashRoutine(Color color, float lifetime)
        {
            screenFlash.color = color;
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / Mathf.Max(0.01f, lifetime));
                screenFlash.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            screenFlash.color = Color.clear;
        }

        private void RequestHitStop(float seconds)
        {
            if (seconds <= 0f || !Application.isPlaying)
            {
                return;
            }

            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                Time.timeScale = originalTimeScale;
            }

            hitStopRoutine = StartCoroutine(HitStopRoutine(seconds));
        }

        private IEnumerator HitStopRoutine(float seconds)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0.08f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            hitStopRoutine = null;
        }
    }
}
