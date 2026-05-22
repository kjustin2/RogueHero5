using System.Collections;
using UnityEngine;

namespace RogueHero5
{
    [DisallowMultipleComponent]
    public sealed class CounterWindow : MonoBehaviour
    {
        [SerializeField] private FighterActor owner;
        [SerializeField] private Health health;

        private float remainingSeconds;
        private int counterDamage;
        private float counterRadius;
        private string moveId;
        private bool triggered;

        public bool IsActive => remainingSeconds > 0f;
        public bool WasTriggered => triggered;

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponent<FighterActor>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health != null)
            {
                health.DamageBlocked += OnDamageBlocked;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.DamageBlocked -= OnDamageBlocked;
            }
        }

        public void Activate(FighterActor newOwner, float duration, int damage, float radius, string newMoveId)
        {
            owner = newOwner;
            remainingSeconds = Mathf.Max(0f, duration);
            counterDamage = Mathf.Max(0, damage);
            counterRadius = Mathf.Max(0.1f, radius);
            moveId = newMoveId;
            triggered = false;

            if (owner != null && owner.InvulnerabilityWindow != null)
            {
                owner.InvulnerabilityWindow.Trigger(remainingSeconds);
            }
        }

        public IEnumerator ResolveFallbackPulse(float fallbackScale)
        {
            while (remainingSeconds > 0f)
            {
                remainingSeconds -= Time.deltaTime;
                yield return null;
            }

            if (!triggered && owner != null)
            {
                Vector3 position = owner.transform.position;
                bool damaged = DamageUtility.ApplyDamageAround(owner, position, counterRadius * fallbackScale, Mathf.CeilToInt(counterDamage * fallbackScale), moveId) > 0;
                MoveDefinition transient = CreateTransientCounterDefinition();
                CombatFeedbackService.Instance?.MoveImpacted(transient, position, damaged);
                Destroy(transient);
            }
        }

        private void OnDamageBlocked(Health blockedHealth, DamageEvent damageEvent)
        {
            if (!IsActive || triggered || owner == null || damageEvent.SourceTeam == owner.Team)
            {
                return;
            }

            triggered = true;
            remainingSeconds = 0f;
            Vector3 position = owner.transform.position;
            DamageUtility.ApplyDamageAround(owner, position, counterRadius, counterDamage, moveId);
            CombatFeedbackService.Instance?.CounterTriggered(owner, position);
        }

        private MoveDefinition CreateTransientCounterDefinition()
        {
            MoveDefinition definition = ScriptableObject.CreateInstance<MoveDefinition>();
            definition.Configure(moveId, "Counter Burst", MoveSlot.Defensive, MoveKind.CounterBurst, 0f, 0f, 0.1f, 0f, counterDamage, 0f, counterRadius, 0f, 0f, 0f, 0f, Color.yellow);
            return definition;
        }
    }
}
