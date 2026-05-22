using System.Collections.Generic;
using UnityEngine;

namespace RogueHero5
{
    [CreateAssetMenu(menuName = "Rogue Hero 5/Duel Slice Presentation Profile", fileName = "DuelSlicePresentationProfile")]
    public sealed class DuelSlicePresentationProfile : ScriptableObject
    {
        [SerializeField] private AbilityPresentationDefinition[] abilityPresentations = new AbilityPresentationDefinition[0];
        [SerializeField] private BossAttackDefinition[] bossAttacks = new BossAttackDefinition[0];
        [SerializeField] private Material arenaFloorMaterial;
        [SerializeField] private Material arenaBorderMaterial;
        [SerializeField] private Material telegraphMaterial;
        [SerializeField] private Material playerMaterial;
        [SerializeField] private Material bossMaterial;
        [SerializeField] private AudioClip ambientLoop;
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip defeatSound;
        [SerializeField] private float arenaRadius = 12f;
        [SerializeField] private Color ambientColor = new Color(0.08f, 0.10f, 0.13f, 1f);

        public IReadOnlyList<AbilityPresentationDefinition> AbilityPresentations => abilityPresentations;
        public IReadOnlyList<BossAttackDefinition> BossAttacks => bossAttacks;
        public Material ArenaFloorMaterial => arenaFloorMaterial;
        public Material ArenaBorderMaterial => arenaBorderMaterial;
        public Material TelegraphMaterial => telegraphMaterial;
        public Material PlayerMaterial => playerMaterial;
        public Material BossMaterial => bossMaterial;
        public AudioClip AmbientLoop => ambientLoop;
        public AudioClip VictorySound => victorySound;
        public AudioClip DefeatSound => defeatSound;
        public float ArenaRadius => arenaRadius;
        public Color AmbientColor => ambientColor;

        public void Configure(
            AbilityPresentationDefinition[] newAbilityPresentations,
            BossAttackDefinition[] newBossAttacks,
            Material newArenaFloorMaterial,
            Material newArenaBorderMaterial,
            Material newTelegraphMaterial,
            Material newPlayerMaterial,
            Material newBossMaterial,
            AudioClip newAmbientLoop,
            AudioClip newVictorySound,
            AudioClip newDefeatSound,
            float newArenaRadius,
            Color newAmbientColor)
        {
            abilityPresentations = newAbilityPresentations ?? new AbilityPresentationDefinition[0];
            bossAttacks = newBossAttacks ?? new BossAttackDefinition[0];
            arenaFloorMaterial = newArenaFloorMaterial;
            arenaBorderMaterial = newArenaBorderMaterial;
            telegraphMaterial = newTelegraphMaterial;
            playerMaterial = newPlayerMaterial;
            bossMaterial = newBossMaterial;
            ambientLoop = newAmbientLoop;
            victorySound = newVictorySound;
            defeatSound = newDefeatSound;
            arenaRadius = Mathf.Max(4f, newArenaRadius);
            ambientColor = newAmbientColor;
        }
    }
}
