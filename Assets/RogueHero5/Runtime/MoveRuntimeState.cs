using UnityEngine;

namespace RogueHero5
{
    public sealed class MoveRuntimeState
    {
        public MoveRuntimeState(MoveDefinition definition)
        {
            Definition = definition;
        }

        public MoveDefinition Definition { get; }
        public float CooldownRemaining { get; private set; }
        public bool IsCoolingDown => CooldownRemaining > 0f;

        public bool CanUse => Definition != null && !IsCoolingDown;

        public void StartCooldown()
        {
            CooldownRemaining = Definition == null ? 0f : Mathf.Max(0f, Definition.CooldownSeconds);
        }

        public void Tick(float deltaSeconds)
        {
            if (CooldownRemaining <= 0f)
            {
                return;
            }

            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Mathf.Max(0f, deltaSeconds));
        }
    }
}
