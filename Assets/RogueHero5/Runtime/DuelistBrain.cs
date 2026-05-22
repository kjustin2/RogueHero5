using System;

namespace RogueHero5
{
    public sealed class DuelistBrain
    {
        private readonly Random random;
        private int decisions;

        public DuelistBrain(int seed)
        {
            random = new Random(seed);
        }

        public int NextAttackIndex(float distanceToPlayer, bool phaseTwo)
        {
            decisions++;

            if (distanceToPlayer <= 2.4f)
            {
                return phaseTwo && decisions % 3 == 0 ? 0 : 1;
            }

            if (phaseTwo && decisions % 4 == 0)
            {
                return 2;
            }

            return random.NextDouble() < 0.55 ? 0 : 2;
        }

        public bool ShouldFeint(bool phaseTwo)
        {
            return phaseTwo && decisions % 4 == 0;
        }
    }
}
