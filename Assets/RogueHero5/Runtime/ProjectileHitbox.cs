using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    public sealed class ProjectileHitbox : MonoBehaviour
    {
        private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
        private FighterActor owner;
        private Vector3 direction;
        private string moveId;
        private float speed;
        private float radius;
        private float remainingLife;
        private int damage;
        private MoveDefinition definition;
        private CombatFeedbackService feedbackService;

        public void Configure(FighterActor newOwner, Vector3 newDirection, MoveDefinition newDefinition, CombatFeedbackService newFeedbackService)
        {
            owner = newOwner;
            direction = newDirection.sqrMagnitude > 0.001f ? newDirection.normalized : transform.forward;
            definition = newDefinition;
            feedbackService = newFeedbackService;
            moveId = definition.MoveId;
            speed = definition.ProjectileSpeed;
            radius = definition.Radius;
            damage = definition.Damage;
            remainingLife = Mathf.Max(0.25f, definition.Range / Mathf.Max(0.1f, speed));
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            float deltaTime = Time.deltaTime;
            transform.position += direction * speed * deltaTime;
            remainingLife -= deltaTime;

            bool damaged = DamageUtility.ApplyDamageAround(owner, transform.position, radius, damage, moveId, alreadyHit) > 0;
            if (damaged)
            {
                feedbackService?.MoveImpacted(definition, transform.position, true);
            }

            if (damaged || remainingLife <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
