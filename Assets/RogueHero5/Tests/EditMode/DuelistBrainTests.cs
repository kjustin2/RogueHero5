using NUnit.Framework;

namespace RogueHero5.Tests
{
    public sealed class DuelistBrainTests
    {
        [Test]
        public void AttackSelectionIsDeterministicForFixedSeed()
        {
            DuelistBrain first = new DuelistBrain(55);
            DuelistBrain second = new DuelistBrain(55);

            for (int i = 0; i < 12; i++)
            {
                bool phaseTwo = i >= 6;
                float distance = i % 3 == 0 ? 1.8f : 6.5f;
                Assert.That(first.NextAttackIndex(distance, phaseTwo), Is.EqualTo(second.NextAttackIndex(distance, phaseTwo)));
                Assert.That(first.ShouldFeint(phaseTwo), Is.EqualTo(second.ShouldFeint(phaseTwo)));
            }
        }
    }
}
