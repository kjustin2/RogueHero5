using NUnit.Framework;
using UnityEngine;

namespace RogueHero5.Tests
{
    public sealed class MoveRuntimeStateTests
    {
        [Test]
        public void CooldownStartsAndTicksToReady()
        {
            MoveDefinition definition = ScriptableObject.CreateInstance<MoveDefinition>();
            definition.Configure("Dodge", "Forward Dodge", MoveSlot.Mobility, MoveKind.Dodge, 1.2f, 0f, 0.15f, 0.1f, 0, 0f, 0.6f, 3f, 0.15f, 0f, 0.3f, Color.white);
            MoveRuntimeState state = new MoveRuntimeState(definition);

            state.StartCooldown();
            Assert.That(state.CanUse, Is.False);
            Assert.That(state.CooldownRemaining, Is.EqualTo(1.2f).Within(0.001f));

            state.Tick(0.5f);
            Assert.That(state.CooldownRemaining, Is.EqualTo(0.7f).Within(0.001f));

            state.Tick(1f);
            Assert.That(state.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
            Assert.That(state.CanUse, Is.True);

            Object.DestroyImmediate(definition);
        }
    }
}
