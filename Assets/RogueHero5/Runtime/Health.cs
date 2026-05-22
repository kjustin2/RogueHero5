using System;
using UnityEngine;

namespace RogueHero5
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private InvulnerabilityWindow invulnerabilityWindow;

        public event Action<Health, DamageEvent> Damaged;
        public event Action<Health, DamageEvent> DamageBlocked;
        public event Action<Health> Died;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;

        private void Awake()
        {
            if (invulnerabilityWindow == null)
            {
                invulnerabilityWindow = GetComponent<InvulnerabilityWindow>();
            }

            if (currentHealth <= 0)
            {
                currentHealth = maxHealth;
            }
        }

        public void Initialize(int newMaxHealth)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            currentHealth = maxHealth;
        }

        public bool ApplyDamage(DamageEvent damageEvent)
        {
            if (invulnerabilityWindow == null)
            {
                invulnerabilityWindow = GetComponent<InvulnerabilityWindow>();
            }

            if (IsDead || damageEvent.Amount <= 0)
            {
                return false;
            }

            if (invulnerabilityWindow != null && invulnerabilityWindow.IsInvulnerable)
            {
                DamageBlocked?.Invoke(this, damageEvent);
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - damageEvent.Amount);
            Damaged?.Invoke(this, damageEvent);

            if (currentHealth == 0)
            {
                Died?.Invoke(this);
            }

            return true;
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }
    }
}
