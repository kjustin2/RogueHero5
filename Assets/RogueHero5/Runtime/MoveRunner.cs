using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    [DisallowMultipleComponent]
    public sealed class MoveRunner : MonoBehaviour
    {
        [SerializeField] private FighterActor owner;
        [SerializeField] private MoveDefinition[] loadout = new MoveDefinition[0];
        [SerializeField] private Material projectileMaterial;
        [SerializeField] private CombatFeedbackService feedbackService;

        private readonly Dictionary<MoveSlot, MoveRuntimeState> statesBySlot = new Dictionary<MoveSlot, MoveRuntimeState>();
        private readonly List<MoveRuntimeState> orderedStates = new List<MoveRuntimeState>();
        private int executingMoveCount;
        private int blockingMoveCount;
        private int movementLockCount;

        public bool IsExecuting => executingMoveCount > 0;
        public bool BlocksMovement => movementLockCount > 0;
        public IReadOnlyList<MoveRuntimeState> OrderedStates => orderedStates;

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponent<FighterActor>();
            }

            RebuildStates();
        }

        private void Update()
        {
            TickCooldowns(Time.deltaTime);
        }

        public void Configure(FighterActor newOwner, MoveDefinition[] newLoadout, Material newProjectileMaterial, CombatFeedbackService newFeedbackService = null)
        {
            owner = newOwner;
            loadout = newLoadout ?? new MoveDefinition[0];
            projectileMaterial = newProjectileMaterial;
            feedbackService = newFeedbackService;
            RebuildStates();
        }

        public bool TryExecute(MoveSlot slot, Vector3 aimDirection)
        {
            if (owner == null || !owner.IsAlive)
            {
                return false;
            }

            if (!statesBySlot.TryGetValue(slot, out MoveRuntimeState state) || !state.CanUse)
            {
                return false;
            }

            if (blockingMoveCount > 0 || state.Definition.BlocksOtherMoves && executingMoveCount > 0)
            {
                return false;
            }

            Vector3 direction = PlanarDirectionOrFallback(aimDirection, transform.forward);
            state.StartCooldown();
            StartCoroutine(ExecuteMove(state.Definition, direction));
            return true;
        }

        public float GetCooldownRemaining(MoveSlot slot)
        {
            return statesBySlot.TryGetValue(slot, out MoveRuntimeState state) ? state.CooldownRemaining : 0f;
        }

        public void TickCooldowns(float deltaSeconds)
        {
            for (int i = 0; i < orderedStates.Count; i++)
            {
                orderedStates[i].Tick(deltaSeconds);
            }
        }

        private void RebuildStates()
        {
            statesBySlot.Clear();
            orderedStates.Clear();

            for (int i = 0; i < loadout.Length; i++)
            {
                MoveDefinition definition = loadout[i];
                if (definition == null || statesBySlot.ContainsKey(definition.Slot))
                {
                    continue;
                }

                MoveRuntimeState state = new MoveRuntimeState(definition);
                statesBySlot.Add(definition.Slot, state);
                orderedStates.Add(state);
            }
        }

        private IEnumerator ExecuteMove(MoveDefinition definition, Vector3 direction)
        {
            executingMoveCount++;
            if (definition.BlocksOtherMoves)
            {
                blockingMoveCount++;
            }

            if (definition.LocksMovement)
            {
                movementLockCount++;
            }

            feedbackService = feedbackService != null ? feedbackService : CombatFeedbackService.Instance;
            feedbackService?.MoveStarted(definition, transform.position, direction);

            if (definition.InvulnerabilitySeconds > 0f && owner.InvulnerabilityWindow != null)
            {
                owner.InvulnerabilityWindow.Trigger(definition.InvulnerabilitySeconds);
            }

            if (definition.Kind == MoveKind.Dodge)
            {
                yield return MoveOverTime(direction, definition.MovementDistance, definition.MovementSeconds);
                yield return WaitSeconds(definition.RecoverySeconds);
                ReleaseMoveLocks(definition);
                yield break;
            }

            yield return WaitSeconds(definition.StartupSeconds);

            switch (definition.Kind)
            {
                case MoveKind.DashStrike:
                    yield return ExecuteDashStrike(definition, direction);
                    break;
                case MoveKind.Projectile:
                    SpawnProjectile(definition, direction);
                    yield return WaitSeconds(definition.ActiveSeconds);
                    break;
                case MoveKind.CounterBurst:
                    yield return ExecuteCounterBurst(definition);
                    break;
                case MoveKind.LeapSlam:
                    yield return MoveOverTime(direction, definition.MovementDistance, definition.MovementSeconds);
                    bool slamDamaged = DamageUtility.ApplyDamageAround(owner, transform.position, definition.Radius, definition.Damage, definition.MoveId) > 0;
                    feedbackService?.MoveImpacted(definition, transform.position, slamDamaged);
                    yield return WaitSeconds(definition.ActiveSeconds);
                    break;
            }

            yield return WaitSeconds(definition.RecoverySeconds);
            ReleaseMoveLocks(definition);
        }

        private IEnumerator ExecuteDashStrike(MoveDefinition definition, Vector3 direction)
        {
            HashSet<Health> alreadyHit = new HashSet<Health>();
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, definition.MovementSeconds);
            float speed = definition.MovementDistance / duration;

            while (elapsed < duration)
            {
                float delta = Mathf.Min(Time.deltaTime, duration - elapsed);
                transform.position += direction * speed * delta;
                transform.forward = direction;
                Vector3 hitCenter = transform.position + direction * 0.7f;
                bool damaged = DamageUtility.ApplyDamageAround(owner, hitCenter, definition.Radius, definition.Damage, definition.MoveId, alreadyHit) > 0;
                if (damaged)
                {
                    feedbackService?.MoveImpacted(definition, hitCenter, true);
                }
                elapsed += delta;
                yield return null;
            }
        }

        private IEnumerator ExecuteCounterBurst(MoveDefinition definition)
        {
            CounterWindow counterWindow = owner.GetComponent<CounterWindow>();
            if (counterWindow == null)
            {
                counterWindow = owner.gameObject.AddComponent<CounterWindow>();
            }

            counterWindow.Activate(owner, Mathf.Max(definition.ActiveSeconds, definition.InvulnerabilitySeconds), definition.Damage, definition.Radius, definition.MoveId);
            yield return counterWindow.ResolveFallbackPulse(0.45f);
        }

        private IEnumerator MoveOverTime(Vector3 direction, float distance, float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            float speed = distance / safeDuration;

            while (elapsed < safeDuration)
            {
                float delta = Mathf.Min(Time.deltaTime, safeDuration - elapsed);
                transform.position += direction * speed * delta;
                transform.forward = direction;
                elapsed += delta;
                yield return null;
            }
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            float remaining = Mathf.Max(0f, seconds);
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
        }

        private void SpawnProjectile(MoveDefinition definition, Vector3 direction)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = definition.DisplayName + " Projectile";
            projectile.transform.position = transform.position + Vector3.up * 1.1f + direction * 0.9f;
            projectile.transform.localScale = Vector3.one * Mathf.Max(0.2f, definition.Radius * 0.6f);

            Collider collider = projectile.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null && projectileMaterial != null)
            {
                renderer.sharedMaterial = projectileMaterial;
            }

            ProjectileHitbox hitbox = projectile.AddComponent<ProjectileHitbox>();
            hitbox.Configure(owner, direction, definition, feedbackService);
        }

        private void ReleaseMoveLocks(MoveDefinition definition)
        {
            executingMoveCount = Mathf.Max(0, executingMoveCount - 1);
            if (definition.BlocksOtherMoves)
            {
                blockingMoveCount = Mathf.Max(0, blockingMoveCount - 1);
            }

            if (definition.LocksMovement)
            {
                movementLockCount = Mathf.Max(0, movementLockCount - 1);
            }
        }

        private static Vector3 PlanarDirectionOrFallback(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            fallback.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }

            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }
    }
}
