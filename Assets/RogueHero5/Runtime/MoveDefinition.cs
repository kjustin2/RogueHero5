using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    [CreateAssetMenu(menuName = "Rogue Hero 5/Move Definition", fileName = "MoveDefinition")]
    public sealed class MoveDefinition : ScriptableObject
    {
        [SerializeField] private string moveId = "move";
        [SerializeField] private string displayName = "Move";
        [SerializeField] private MoveSlot slot;
        [SerializeField] private MoveKind kind;
        [SerializeField] private float cooldownSeconds = 1f;
        [SerializeField] private float startupSeconds = 0.05f;
        [SerializeField] private float activeSeconds = 0.15f;
        [SerializeField] private float recoverySeconds = 0.1f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float range = 2f;
        [SerializeField] private float radius = 0.8f;
        [SerializeField] private float movementDistance = 3f;
        [SerializeField] private float movementSeconds = 0.16f;
        [SerializeField] private float projectileSpeed = 16f;
        [SerializeField] private float invulnerabilitySeconds;
        [SerializeField] private Color effectColor = Color.white;
        [SerializeField] private bool locksMovement = true;
        [SerializeField] private bool blocksOtherMoves = true;
        [SerializeField] private float inputBufferSeconds = 0.16f;

        public string MoveId => moveId;
        public string DisplayName => displayName;
        public MoveSlot Slot => slot;
        public MoveKind Kind => kind;
        public float CooldownSeconds => cooldownSeconds;
        public float StartupSeconds => startupSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverySeconds => recoverySeconds;
        public int Damage => damage;
        public float Range => range;
        public float Radius => radius;
        public float MovementDistance => movementDistance;
        public float MovementSeconds => movementSeconds;
        public float ProjectileSpeed => projectileSpeed;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public Color EffectColor => effectColor;
        public bool LocksMovement => locksMovement;
        public bool BlocksOtherMoves => blocksOtherMoves;
        public float InputBufferSeconds => inputBufferSeconds;

        public void Configure(
            string newMoveId,
            string newDisplayName,
            MoveSlot newSlot,
            MoveKind newKind,
            float newCooldownSeconds,
            float newStartupSeconds,
            float newActiveSeconds,
            float newRecoverySeconds,
            int newDamage,
            float newRange,
            float newRadius,
            float newMovementDistance,
            float newMovementSeconds,
            float newProjectileSpeed,
            float newInvulnerabilitySeconds,
            Color newEffectColor)
        {
            moveId = newMoveId;
            displayName = newDisplayName;
            slot = newSlot;
            kind = newKind;
            cooldownSeconds = newCooldownSeconds;
            startupSeconds = newStartupSeconds;
            activeSeconds = newActiveSeconds;
            recoverySeconds = newRecoverySeconds;
            damage = newDamage;
            range = newRange;
            radius = newRadius;
            movementDistance = newMovementDistance;
            movementSeconds = newMovementSeconds;
            projectileSpeed = newProjectileSpeed;
            invulnerabilitySeconds = newInvulnerabilitySeconds;
            effectColor = newEffectColor;
        }

        public void ConfigureCommitment(bool newLocksMovement, bool newBlocksOtherMoves, float newInputBufferSeconds)
        {
            locksMovement = newLocksMovement;
            blocksOtherMoves = newBlocksOtherMoves;
            inputBufferSeconds = Mathf.Max(0f, newInputBufferSeconds);
        }

        public IReadOnlyList<string> ValidateDefinition()
        {
            List<string> issues = new List<string>();

            if (string.IsNullOrWhiteSpace(moveId))
            {
                issues.Add("Move id is required.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                issues.Add("Display name is required.");
            }

            if (cooldownSeconds < 0f)
            {
                issues.Add("Cooldown cannot be negative.");
            }

            if (startupSeconds < 0f || activeSeconds <= 0f || recoverySeconds < 0f)
            {
                issues.Add("Startup, active, and recovery timing must be valid.");
            }

            if (damage < 0)
            {
                issues.Add("Damage cannot be negative.");
            }

            if (range < 0f)
            {
                issues.Add("Range cannot be negative.");
            }

            if (radius <= 0f)
            {
                issues.Add("Radius must be positive.");
            }

            if ((kind == MoveKind.Dodge || kind == MoveKind.DashStrike || kind == MoveKind.LeapSlam) && movementDistance <= 0f)
            {
                issues.Add("Movement distance is required for movement moves.");
            }

            if ((kind == MoveKind.Dodge || kind == MoveKind.DashStrike || kind == MoveKind.LeapSlam) && movementSeconds <= 0f)
            {
                issues.Add("Movement seconds is required for movement moves.");
            }

            if (kind == MoveKind.Projectile && projectileSpeed <= 0f)
            {
                issues.Add("Projectile speed is required for projectile moves.");
            }

            return issues;
        }
    }
}
