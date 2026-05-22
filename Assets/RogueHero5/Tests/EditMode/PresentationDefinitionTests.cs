using NUnit.Framework;
using UnityEngine;

namespace RogueHero5.Tests
{
    public sealed class PresentationDefinitionTests
    {
        [Test]
        public void AbilityPresentationValidatesFeedbackValues()
        {
            AbilityPresentationDefinition definition = ScriptableObject.CreateInstance<AbilityPresentationDefinition>();
            definition.Configure(MoveSlot.Primary, "Spear Dash", null, Color.cyan, Color.white, null, null, 0.8f, 1.2f, 0.15f, 0.04f, 0.08f, 1f);

            Assert.That(definition.ValidateDefinition(), Is.Empty);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void BossAttackDefinitionRequiresReadableTimingAndDamage()
        {
            BossAttackDefinition definition = ScriptableObject.CreateInstance<BossAttackDefinition>();
            definition.Configure("dash", "Dash Slash", BossAttackKind.DashSlash, 0.7f, 0.2f, 0.5f, 12, 5f, 0.8f, Color.red, Color.red, null, null);

            Assert.That(definition.ValidateDefinition(), Is.Empty);

            Object.DestroyImmediate(definition);
        }
    }
}
