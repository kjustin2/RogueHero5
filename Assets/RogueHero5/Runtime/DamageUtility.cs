using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    public static class DamageUtility
    {
        private static readonly Collider[] Hits = new Collider[32];

        public static int ApplyDamageAround(
            FighterActor owner,
            Vector3 center,
            float radius,
            int damage,
            string moveId,
            HashSet<Health> alreadyHit = null)
        {
            if (owner == null || damage <= 0 || radius <= 0f)
            {
                return 0;
            }

            int count = Physics.OverlapSphereNonAlloc(center, radius, Hits, ~0, QueryTriggerInteraction.Collide);
            int damaged = 0;

            for (int i = 0; i < count; i++)
            {
                Collider hit = Hits[i];
                if (hit == null)
                {
                    continue;
                }

                FighterActor target = hit.GetComponentInParent<FighterActor>();
                if (target == null || target == owner || target.Team == owner.Team || !target.IsAlive)
                {
                    continue;
                }

                Health targetHealth = target.Health;
                if (targetHealth == null || alreadyHit != null && alreadyHit.Contains(targetHealth))
                {
                    continue;
                }

                if (targetHealth.ApplyDamage(new DamageEvent(damage, owner.Team, owner.gameObject, center, moveId)))
                {
                    alreadyHit?.Add(targetHealth);
                    damaged++;
                }
            }

            return damaged;
        }
    }
}
