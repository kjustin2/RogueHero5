using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueHero5.Tests
{
    public sealed class MoveRunnerPlayModeTests
    {
        [UnityTest]
        public IEnumerator DodgeMovesActorAndBlocksDamageDuringIFrames()
        {
            GameObject player = CreateActor("Player", ActorTeam.Player, new Vector3(0f, 1f, 0f), 100, out FighterActor playerActor);
            GameObject enemy = CreateActor("Enemy", ActorTeam.Enemy, new Vector3(0f, 1f, 3f), 100, out _);
            MoveDefinition dodge = CreateMove("ForwardDodge", MoveSlot.Mobility, MoveKind.Dodge, 0.2f, 0, 0f, 0.16f, 0.05f, 0, 0f, 0.6f, 2.5f, 0.16f, 0f, 0.35f);
            MoveRunner runner = player.AddComponent<MoveRunner>();
            runner.Configure(playerActor, new[] { dodge }, null);

            Vector3 start = player.transform.position;
            Assert.That(runner.TryExecute(MoveSlot.Mobility, Vector3.forward), Is.True);

            bool blocked = playerActor.Health.ApplyDamage(new DamageEvent(20, ActorTeam.Enemy, enemy, player.transform.position, "test"));
            Assert.That(blocked, Is.False);
            Assert.That(playerActor.Health.CurrentHealth, Is.EqualTo(100));

            yield return new WaitForSeconds(0.24f);
            Assert.That(player.transform.position.z - start.z, Is.GreaterThan(1.5f));

            yield return new WaitForSeconds(0.22f);
            bool applied = playerActor.Health.ApplyDamage(new DamageEvent(20, ActorTeam.Enemy, enemy, player.transform.position, "test"));
            Assert.That(applied, Is.True);

            Object.Destroy(player);
            Object.Destroy(enemy);
            Object.Destroy(dodge);
        }

        [UnityTest]
        public IEnumerator DashStrikeDamagesEnemyOnce()
        {
            GameObject player = CreateActor("Player", ActorTeam.Player, new Vector3(0f, 1f, 0f), 100, out FighterActor playerActor);
            GameObject enemy = CreateActor("Enemy", ActorTeam.Enemy, new Vector3(0f, 1f, 2.2f), 100, out FighterActor enemyActor);
            MoveDefinition dash = CreateMove("SpearDash", MoveSlot.Primary, MoveKind.DashStrike, 0.2f, 25, 0.02f, 0.08f, 0.05f, 25, 3f, 1.0f, 2.8f, 0.12f, 0f, 0.05f);
            MoveRunner runner = player.AddComponent<MoveRunner>();
            runner.Configure(playerActor, new[] { dash }, null);

            Assert.That(runner.TryExecute(MoveSlot.Primary, Vector3.forward), Is.True);
            yield return new WaitForSeconds(0.25f);

            Assert.That(enemyActor.Health.CurrentHealth, Is.EqualTo(75));

            Object.Destroy(player);
            Object.Destroy(enemy);
            Object.Destroy(dash);
        }

        [UnityTest]
        public IEnumerator CounterBurstBlocksHitAndReturnsDamage()
        {
            GameObject player = CreateActor("Player", ActorTeam.Player, new Vector3(0f, 1f, 0f), 100, out FighterActor playerActor);
            GameObject enemy = CreateActor("Enemy", ActorTeam.Enemy, new Vector3(0f, 1f, 1.2f), 100, out FighterActor enemyActor);
            MoveDefinition counter = CreateMove("CounterBurst", MoveSlot.Defensive, MoveKind.CounterBurst, 0.2f, 30, 0.0f, 0.28f, 0.05f, 30, 0f, 1.8f, 0f, 0f, 0f, 0.28f);
            counter.ConfigureCommitment(false, false, 0.16f);
            MoveRunner runner = player.AddComponent<MoveRunner>();
            runner.Configure(playerActor, new[] { counter }, null);

            Assert.That(runner.TryExecute(MoveSlot.Defensive, Vector3.forward), Is.True);
            yield return null;

            bool applied = playerActor.Health.ApplyDamage(new DamageEvent(25, ActorTeam.Enemy, enemy, player.transform.position, "enemy_hit"));
            Assert.That(applied, Is.False);

            yield return new WaitForSeconds(0.08f);

            Assert.That(playerActor.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(enemyActor.Health.CurrentHealth, Is.LessThan(100));

            Object.Destroy(player);
            Object.Destroy(enemy);
            Object.Destroy(counter);
        }

        private static GameObject CreateActor(string name, ActorTeam team, Vector3 position, int maxHealth, out FighterActor actor)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gameObject.name = name;
            gameObject.transform.position = position;
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            gameObject.AddComponent<InvulnerabilityWindow>();
            Health health = gameObject.AddComponent<Health>();
            health.Initialize(maxHealth);
            actor = gameObject.AddComponent<FighterActor>();
            actor.Configure(team, 0.65f);
            Physics.SyncTransforms();
            return gameObject;
        }

        private static MoveDefinition CreateMove(
            string id,
            MoveSlot slot,
            MoveKind kind,
            float cooldown,
            int damage,
            float startup,
            float active,
            float recovery,
            int ignoredDamage,
            float range,
            float radius,
            float movementDistance,
            float movementSeconds,
            float projectileSpeed,
            float invulnerability)
        {
            MoveDefinition definition = ScriptableObject.CreateInstance<MoveDefinition>();
            definition.Configure(id, id, slot, kind, cooldown, startup, active, recovery, damage, range, radius, movementDistance, movementSeconds, projectileSpeed, invulnerability, Color.white);
            return definition;
        }
    }
}
