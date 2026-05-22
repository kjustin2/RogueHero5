using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    public sealed class DuelistBossController : MonoBehaviour
    {
        private enum BossState
        {
            Idle,
            Windup,
            Active,
            Recovery
        }

        [SerializeField] private FighterActor self;
        [SerializeField] private FighterActor target;
        [SerializeField] private Material telegraphMaterial;
        [SerializeField] private BossAttackDefinition[] attackDefinitions = new BossAttackDefinition[0];
        [SerializeField] private float idleDelay = 0.45f;
        [SerializeField] private int brainSeed = 55;

        private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
        private readonly bool[] comboHitResolved = new bool[3];

        private DuelistBrain brain;
        private BossState state;
        private BossAttackDefinition currentAttack;
        private Vector3 cachedDirection;
        private float stateTimer;
        private float idleTimer;
        private bool currentFeint;
        private bool feintResolved;
        private GameObject telegraph;
        private Renderer telegraphRenderer;
        private BossAttackDefinition[] fallbackAttacks;

        public FighterActor Target => target;
        public bool IsPhaseTwo => self != null && self.Health != null && self.Health.CurrentHealth <= self.Health.MaxHealth / 2;

        private void Awake()
        {
            if (self == null)
            {
                self = GetComponent<FighterActor>();
            }

            brain = new DuelistBrain(brainSeed);
            idleTimer = idleDelay;
        }

        public void Configure(FighterActor newSelf, FighterActor newTarget, Material newTelegraphMaterial, int newBrainSeed, BossAttackDefinition[] newAttackDefinitions = null)
        {
            self = newSelf;
            target = newTarget;
            telegraphMaterial = newTelegraphMaterial;
            brainSeed = newBrainSeed;
            attackDefinitions = newAttackDefinitions ?? attackDefinitions;
            brain = new DuelistBrain(brainSeed);
        }

        private void Update()
        {
            if (self == null || target == null || !self.IsAlive || !target.IsAlive)
            {
                HideTelegraph();
                return;
            }

            switch (state)
            {
                case BossState.Idle:
                    UpdateIdle();
                    break;
                case BossState.Windup:
                    UpdateWindup();
                    break;
                case BossState.Active:
                    UpdateActive();
                    break;
                case BossState.Recovery:
                    UpdateRecovery();
                    break;
            }
        }

        private void UpdateIdle()
        {
            FaceTarget();
            idleTimer -= Time.deltaTime;
            if (idleTimer > 0f)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            int attackIndex = brain.NextAttackIndex(distance, IsPhaseTwo);
            currentAttack = GetAttack(attackIndex);
            currentFeint = currentAttack.Kind == BossAttackKind.DashSlash && brain.ShouldFeint(IsPhaseTwo);
            feintResolved = false;
            cachedDirection = PlanarDirectionToTarget();
            alreadyHit.Clear();
            for (int i = 0; i < comboHitResolved.Length; i++)
            {
                comboHitResolved[i] = false;
            }

            state = BossState.Windup;
            stateTimer = 0f;
            ShowTelegraph();
            CombatFeedbackService.Instance?.BossWarning(currentAttack, transform.position);
        }

        private void UpdateWindup()
        {
            stateTimer += Time.deltaTime;
            UpdateTelegraphTransform();

            if (currentFeint && !feintResolved && stateTimer >= currentAttack.TelegraphSeconds * 0.55f)
            {
                HideTelegraph();
                feintResolved = true;
                stateTimer = -0.22f;
                cachedDirection = PlanarDirectionToTarget();
                CombatFeedbackService.Instance?.BossWarning(currentAttack, transform.position);
                return;
            }

            if (stateTimer < currentAttack.TelegraphSeconds)
            {
                return;
            }

            HideTelegraph();
            state = BossState.Active;
            stateTimer = 0f;
        }

        private void UpdateActive()
        {
            stateTimer += Time.deltaTime;

            switch (currentAttack.Kind)
            {
                case BossAttackKind.DashSlash:
                case BossAttackKind.Lunge:
                    MoveAlongAttackLine();
                    Vector3 center = transform.position + cachedDirection * 0.85f;
                    bool damaged = DamageUtility.ApplyDamageAround(self, center, currentAttack.Radius, currentAttack.Damage, currentAttack.DisplayName, alreadyHit) > 0;
                    CombatFeedbackService.Instance?.BossImpact(currentAttack, center, damaged);
                    break;
                case BossAttackKind.Combo:
                    ResolveComboHit(0, 0.12f);
                    ResolveComboHit(1, 0.42f);
                    ResolveComboHit(2, 0.72f);
                    break;
            }

            if (stateTimer >= currentAttack.ActiveSeconds)
            {
                state = BossState.Recovery;
                stateTimer = 0f;
            }
        }

        private void UpdateRecovery()
        {
            FaceTarget();
            stateTimer += Time.deltaTime;
            float phaseModifier = IsPhaseTwo ? 0.72f : 1f;
            if (stateTimer < currentAttack.RecoverySeconds * phaseModifier)
            {
                return;
            }

            idleTimer = idleDelay * phaseModifier;
            state = BossState.Idle;
        }

        private void ResolveComboHit(int index, float normalizedTime)
        {
            if (comboHitResolved[index] || stateTimer < currentAttack.ActiveSeconds * normalizedTime)
            {
                return;
            }

            comboHitResolved[index] = true;
            alreadyHit.Clear();
            Vector3 center = transform.position + transform.forward * currentAttack.Range;
            bool damaged = DamageUtility.ApplyDamageAround(self, center, currentAttack.Radius, currentAttack.Damage, currentAttack.DisplayName, alreadyHit) > 0;
            CombatFeedbackService.Instance?.BossImpact(currentAttack, center, damaged);
        }

        private void MoveAlongAttackLine()
        {
            float speed = currentAttack.Range / Mathf.Max(0.05f, currentAttack.ActiveSeconds);
            transform.position += cachedDirection * speed * Time.deltaTime;
            transform.forward = cachedDirection;
        }

        private void FaceTarget()
        {
            Vector3 direction = PlanarDirectionToTarget();
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.forward = direction;
            }
        }

        private Vector3 PlanarDirectionToTarget()
        {
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        }

        private void ShowTelegraph()
        {
            if (telegraph == null)
            {
                telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
                telegraph.name = "Duelist Telegraph";
                Collider collider = telegraph.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = telegraph.GetComponent<Renderer>();
                if (renderer != null && telegraphMaterial != null)
                {
                    renderer.sharedMaterial = telegraphMaterial;
                }

                telegraphRenderer = renderer;
            }

            telegraph.SetActive(true);
            UpdateTelegraphTransform();
        }

        private void HideTelegraph()
        {
            if (telegraph != null)
            {
                telegraph.SetActive(false);
            }
        }

        private void UpdateTelegraphTransform()
        {
            if (telegraph == null)
            {
                return;
            }

            Vector3 center = transform.position + cachedDirection * (currentAttack.Range * 0.5f);
            if (currentAttack.Kind == BossAttackKind.Combo)
            {
                center = transform.position + cachedDirection * currentAttack.Range;
                telegraph.transform.localScale = new Vector3(currentAttack.Radius * 2f, 0.05f, currentAttack.Radius * 2f);
            }
            else
            {
                telegraph.transform.localScale = new Vector3(currentAttack.Radius * 2f, 0.05f, currentAttack.Range);
            }

            telegraph.transform.position = center + Vector3.up * 0.04f;
            telegraph.transform.rotation = Quaternion.LookRotation(cachedDirection, Vector3.up);

            if (telegraphRenderer != null)
            {
                float pulse = 0.65f + Mathf.PingPong(Time.time * 4.5f, 0.35f);
                telegraphRenderer.material.color = new Color(currentAttack.TelegraphColor.r, currentAttack.TelegraphColor.g, currentAttack.TelegraphColor.b, pulse);
            }
        }

        private BossAttackDefinition GetAttack(int index)
        {
            if (attackDefinitions != null && attackDefinitions.Length > 0)
            {
                return attackDefinitions[Mathf.Clamp(index, 0, attackDefinitions.Length - 1)];
            }

            if (fallbackAttacks == null)
            {
                fallbackAttacks = new[]
                {
                    ScriptableObject.CreateInstance<BossAttackDefinition>(),
                    ScriptableObject.CreateInstance<BossAttackDefinition>(),
                    ScriptableObject.CreateInstance<BossAttackDefinition>()
                };
                fallbackAttacks[0].Configure("dash_slash", "Red-Line Dash Slash", BossAttackKind.DashSlash, 0.72f, 0.20f, 0.55f, 14, 5.5f, 0.85f, Color.red, Color.red, null, null);
                fallbackAttacks[1].Configure("combo", "Close Three-Hit Pattern", BossAttackKind.Combo, 0.38f, 0.92f, 0.70f, 9, 1.65f, 1.05f, Color.red, Color.red, null, null);
                fallbackAttacks[2].Configure("lunge", "Arena-Crossing Lunge", BossAttackKind.Lunge, 1.05f, 0.30f, 0.80f, 22, 8.0f, 0.95f, Color.red, Color.red, null, null);
            }

            return fallbackAttacks[Mathf.Clamp(index, 0, fallbackAttacks.Length - 1)];
        }
    }
}
