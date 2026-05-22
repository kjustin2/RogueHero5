using NUnit.Framework;
using UnityEngine;

namespace RogueHero5.Tests
{
    public sealed class HealthTests
    {
        [Test]
        public void InvulnerabilityBlocksDamageUntilWindowExpires()
        {
            GameObject actor = new GameObject("Actor");
            InvulnerabilityWindow invulnerability = actor.AddComponent<InvulnerabilityWindow>();
            Health health = actor.AddComponent<Health>();
            health.Initialize(100);

            invulnerability.Trigger(0.5f);

            bool blocked = health.ApplyDamage(new DamageEvent(25, ActorTeam.Enemy, null, Vector3.zero, "test"));
            Assert.That(blocked, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));

            invulnerability.ManualTick(0.6f);
            bool applied = health.ApplyDamage(new DamageEvent(25, ActorTeam.Enemy, null, Vector3.zero, "test"));
            Assert.That(applied, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(75));

            Object.DestroyImmediate(actor);
        }
    }
}
