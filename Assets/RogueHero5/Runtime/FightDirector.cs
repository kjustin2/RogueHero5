using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RogueHero5
{
    public sealed class FightDirector : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health bossHealth;
        [SerializeField] private ThirdPersonPlayerController playerController;
        [SerializeField] private HudController hudController;
        [SerializeField] private DuelSlicePresentationProfile presentationProfile;
        [SerializeField] private AudioSource uiAudioSource;

        public FightState State { get; private set; }
        public bool IsEnded => State != FightState.Running;

        public void Configure(Health newPlayerHealth, Health newBossHealth, ThirdPersonPlayerController newPlayerController, HudController newHudController, DuelSlicePresentationProfile newPresentationProfile = null, AudioSource newUiAudioSource = null)
        {
            playerHealth = newPlayerHealth;
            bossHealth = newBossHealth;
            playerController = newPlayerController;
            hudController = newHudController;
            presentationProfile = newPresentationProfile;
            uiAudioSource = newUiAudioSource;
        }

        private void Start()
        {
            State = FightState.Running;
            hudController?.SetMessage("Defeat The Duelist");

            if (playerHealth != null)
            {
                playerHealth.Damaged += OnActorDamaged;
            }

            if (bossHealth != null)
            {
                bossHealth.Damaged += OnActorDamaged;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnActorDamaged;
            }

            if (bossHealth != null)
            {
                bossHealth.Damaged -= OnActorDamaged;
            }
        }

        private void Update()
        {
            if (State == FightState.Running)
            {
                if (bossHealth != null && bossHealth.IsDead)
                {
                    EndFight(FightState.Victory, "VICTORY - Press R to restart");
                }
                else if (playerHealth != null && playerHealth.IsDead)
                {
                    EndFight(FightState.Defeat, "DEFEAT - Press R to restart");
                }
            }
            else if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void EndFight(FightState newState, string message)
        {
            State = newState;
            hudController?.SetMessage(message);
            playerController?.SetInputLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            AudioClip clip = newState == FightState.Victory ? presentationProfile?.VictorySound : presentationProfile?.DefeatSound;
            if (clip != null && uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(clip, 0.85f);
            }
        }

        private void OnActorDamaged(Health health, DamageEvent damageEvent)
        {
            FighterActor actor = health.GetComponent<FighterActor>();
            CombatFeedbackService.Instance?.ActorDamaged(actor, damageEvent);
        }
    }
}
