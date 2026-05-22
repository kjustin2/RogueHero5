using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RogueHero5
{
    [System.Serializable]
    public sealed class AbilityHudSlot
    {
        public MoveSlot Slot;
        public Image Icon;
        public Image CooldownFill;
        public Text Label;
        public Text CooldownText;
        public AbilityPresentationDefinition Presentation;
    }

    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health bossHealth;
        [SerializeField] private MoveRunner moveRunner;
        [SerializeField] private Image playerHealthFill;
        [SerializeField] private Image bossHealthFill;
        [SerializeField] private Text playerHealthText;
        [SerializeField] private Text bossHealthText;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text messageText;
        [SerializeField] private AbilityHudSlot[] abilityHudSlots = new AbilityHudSlot[0];

        private readonly StringBuilder cooldownBuilder = new StringBuilder(160);
        private string persistentMessage = string.Empty;

        public void Configure(
            Health newPlayerHealth,
            Health newBossHealth,
            MoveRunner newMoveRunner,
            Image newPlayerHealthFill,
            Image newBossHealthFill,
            Text newPlayerHealthText,
            Text newBossHealthText,
            Text newCooldownText,
            Text newMessageText,
            AbilityHudSlot[] newAbilityHudSlots = null)
        {
            playerHealth = newPlayerHealth;
            bossHealth = newBossHealth;
            moveRunner = newMoveRunner;
            playerHealthFill = newPlayerHealthFill;
            bossHealthFill = newBossHealthFill;
            playerHealthText = newPlayerHealthText;
            bossHealthText = newBossHealthText;
            cooldownText = newCooldownText;
            messageText = newMessageText;
            abilityHudSlots = newAbilityHudSlots ?? new AbilityHudSlot[0];
        }

        public void SetMessage(string message)
        {
            persistentMessage = message;
            if (messageText != null)
            {
                messageText.text = persistentMessage;
            }
        }

        public void ConfigureRuntimeMoveRunner(MoveRunner newMoveRunner)
        {
            moveRunner = newMoveRunner;
        }

        private void Update()
        {
            UpdateHealth(playerHealth, playerHealthFill, playerHealthText, "Player");
            UpdateHealth(bossHealth, bossHealthFill, bossHealthText, "Duelist");
            UpdateCooldowns();
            UpdateAbilityBar();
        }

        private static void UpdateHealth(Health health, Image fill, Text label, string displayName)
        {
            if (health == null)
            {
                return;
            }

            float normalized = health.MaxHealth <= 0 ? 0f : (float)health.CurrentHealth / health.MaxHealth;
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(normalized);
            }

            if (label != null)
            {
                label.text = $"{displayName}: {health.CurrentHealth}/{health.MaxHealth}";
            }
        }

        private void UpdateCooldowns()
        {
            if (cooldownText == null || moveRunner == null)
            {
                return;
            }

            cooldownBuilder.Clear();
            cooldownBuilder.AppendLine("LMB Spear Dash | RMB Void Spear");
            cooldownBuilder.AppendLine("Space Dodge | E Counter | R Meteor");

            IReadOnlyList<MoveRuntimeState> states = moveRunner.OrderedStates;
            for (int i = 0; i < states.Count; i++)
            {
                MoveRuntimeState state = states[i];
                if (state.Definition == null)
                {
                    continue;
                }

                cooldownBuilder.Append(state.Definition.DisplayName);
                cooldownBuilder.Append(": ");
                cooldownBuilder.Append(state.CooldownRemaining <= 0.01f ? "ready" : state.CooldownRemaining.ToString("0.0s"));
                if (i < states.Count - 1)
                {
                    cooldownBuilder.Append("   ");
                }
            }

            cooldownText.text = cooldownBuilder.ToString();
            if (messageText != null)
            {
                messageText.text = persistentMessage;
            }
        }

        private void UpdateAbilityBar()
        {
            if (moveRunner == null || abilityHudSlots == null)
            {
                return;
            }

            for (int i = 0; i < abilityHudSlots.Length; i++)
            {
                AbilityHudSlot slot = abilityHudSlots[i];
                if (slot == null)
                {
                    continue;
                }

                float remaining = moveRunner.GetCooldownRemaining(slot.Slot);
                if (slot.CooldownFill != null)
                {
                    slot.CooldownFill.fillAmount = remaining > 0.01f ? Mathf.Clamp01(remaining / 5.5f) : 0f;
                }

                if (slot.CooldownText != null)
                {
                    slot.CooldownText.text = remaining > 0.01f ? remaining.ToString("0.0") : string.Empty;
                }

                if (slot.Label != null && slot.Presentation != null)
                {
                    slot.Label.text = slot.Presentation.DisplayName;
                }

                if (slot.Icon != null && slot.Presentation != null)
                {
                    slot.Icon.color = remaining > 0.01f ? new Color(0.55f, 0.60f, 0.68f, 0.9f) : Color.white;
                }
            }
        }
    }
}
