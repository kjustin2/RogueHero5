using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueHero5.Tests
{
    public sealed class DuelistBossPlayModeTests
    {
        [UnityTest]
        public IEnumerator DuelistTelegraphPrecedesDamage()
        {
            GameObject boss = CreateActor("The Duelist", ActorTeam.Enemy, new Vector3(0f, 1f, 0f), 220, out FighterActor bossActor);
            GameObject player = CreateActor("Player", ActorTeam.Player, new Vector3(0f, 1f, 1.5f), 100, out FighterActor playerActor);
            DuelistBossController controller = boss.AddComponent<DuelistBossController>();
            controller.Configure(bossActor, playerActor, null, 55);

            yield return new WaitForSeconds(0.50f);
            Assert.That(playerActor.Health.CurrentHealth, Is.EqualTo(100));

            yield return new WaitForSeconds(0.70f);
            Assert.That(playerActor.Health.CurrentHealth, Is.LessThan(100));

            Object.Destroy(boss);
            Object.Destroy(player);
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
    }
}
