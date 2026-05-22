using NUnit.Framework;
using UnityEngine;

namespace RogueHero5.Tests
{
    public sealed class MoveDefinitionValidationTests
    {
        [Test]
        public void ValidMoveDefinitionHasNoValidationIssues()
        {
            MoveDefinition definition = ScriptableObject.CreateInstance<MoveDefinition>();
            definition.Configure(
                "SpearDash",
                "Spear Dash",
                MoveSlot.Primary,
                MoveKind.DashStrike,
                1f,
                0.05f,
                0.15f,
                0.1f,
                12,
                3f,
                0.75f,
                3f,
                0.15f,
                0f,
                0.05f,
                Color.cyan);

            Assert.That(definition.ValidateDefinition(), Is.Empty);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void InvalidMoveDefinitionReportsRequiredFields()
        {
            MoveDefinition definition = ScriptableObject.CreateInstance<MoveDefinition>();
            definition.Configure(
                string.Empty,
                string.Empty,
                MoveSlot.Primary,
                MoveKind.Projectile,
                -1f,
                -0.1f,
                0f,
                -0.1f,
                -1,
                -1f,
                0f,
                0f,
                0f,
                0f,
                0f,
                Color.white);

            Assert.That(definition.ValidateDefinition().Count, Is.GreaterThanOrEqualTo(6));

            Object.DestroyImmediate(definition);
        }
    }
}
