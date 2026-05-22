using UnityEngine;

namespace RogueHero5
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class FighterActor : MonoBehaviour
    {
        [SerializeField] private ActorTeam team;
        [SerializeField] private float bodyRadius = 0.65f;
        [SerializeField] private Health health;
        [SerializeField] private InvulnerabilityWindow invulnerabilityWindow;

        public ActorTeam Team => team;
        public float BodyRadius => bodyRadius;
        public Health Health => health;
        public InvulnerabilityWindow InvulnerabilityWindow => invulnerabilityWindow;
        public bool IsAlive => health != null && !health.IsDead;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
            bodyRadius = Mathf.Max(0.1f, bodyRadius);
        }

        public void Configure(ActorTeam newTeam, float newBodyRadius)
        {
            team = newTeam;
            bodyRadius = Mathf.Max(0.1f, newBodyRadius);
            CacheReferences();
        }

        private void CacheReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (invulnerabilityWindow == null)
            {
                invulnerabilityWindow = GetComponent<InvulnerabilityWindow>();
            }
        }
    }
}
