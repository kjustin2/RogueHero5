using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    [CreateAssetMenu(menuName = "Rogue Hero 5/Boss Attack Definition", fileName = "BossAttack")]
    public sealed class BossAttackDefinition : ScriptableObject
    {
        [SerializeField] private string attackId = "boss_attack";
        [SerializeField] private string displayName = "Boss Attack";
        [SerializeField] private BossAttackKind kind;
        [SerializeField] private float telegraphSeconds = 0.7f;
        [SerializeField] private float activeSeconds = 0.2f;
        [SerializeField] private float recoverySeconds = 0.55f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float range = 5f;
        [SerializeField] private float radius = 0.8f;
        [SerializeField] private Color telegraphColor = Color.red;
        [SerializeField] private Color impactColor = Color.red;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip impactSound;

        public string AttackId => attackId;
        public string DisplayName => displayName;
        public BossAttackKind Kind => kind;
        public float TelegraphSeconds => telegraphSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverySeconds => recoverySeconds;
        public int Damage => damage;
        public float Range => range;
        public float Radius => radius;
        public Color TelegraphColor => telegraphColor;
        public Color ImpactColor => impactColor;
        public AudioClip WarningSound => warningSound;
        public AudioClip ImpactSound => impactSound;

        public void Configure(
            string newAttackId,
            string newDisplayName,
            BossAttackKind newKind,
            float newTelegraphSeconds,
            float newActiveSeconds,
            float newRecoverySeconds,
            int newDamage,
            float newRange,
            float newRadius,
            Color newTelegraphColor,
            Color newImpactColor,
            AudioClip newWarningSound,
            AudioClip newImpactSound)
        {
            attackId = newAttackId;
            displayName = newDisplayName;
            kind = newKind;
            telegraphSeconds = newTelegraphSeconds;
            activeSeconds = newActiveSeconds;
            recoverySeconds = newRecoverySeconds;
            damage = newDamage;
            range = newRange;
            radius = newRadius;
            telegraphColor = newTelegraphColor;
            impactColor = newImpactColor;
            warningSound = newWarningSound;
            impactSound = newImpactSound;
        }

        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new List<string>();
            if (string.IsNullOrWhiteSpace(attackId))
            {
                issues.Add("Attack id is required.");
            }

            if (telegraphSeconds <= 0f || activeSeconds <= 0f || recoverySeconds < 0f)
            {
                issues.Add("Attack timing must include telegraph, active, and non-negative recovery.");
            }

            if (damage <= 0)
            {
                issues.Add("Boss attacks must damage the player.");
            }

            if (range <= 0f || radius <= 0f)
            {
                issues.Add("Boss attack range and radius must be positive.");
            }

            return issues;
        }
    }
}
