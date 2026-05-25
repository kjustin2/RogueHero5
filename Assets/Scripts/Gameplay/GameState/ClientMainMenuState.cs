using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.UI;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Sessions;
using Unity.BossRoom.Utils;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using VContainer.Unity;
using Avatar = Unity.BossRoom.Gameplay.Configuration.Avatar;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states.
    /// </summary>
    /// <remarks> OnNetworkSpawn() won't ever run, because there is no network connection at the main menu screen.
    /// Fortunately we know you are a client, because all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.MainMenu;

        [SerializeField]
        NameGenerationData m_NameGenerationData;
        [SerializeField]
        SessionUIMediator m_SessionUIMediator;
        [SerializeField]
        IPUIMediator m_IPUIMediator;
        [SerializeField]
        Button m_SessionButton;
        [SerializeField]
        GameObject m_SignInSpinner;
        [SerializeField]
        UIProfileSelector m_UIProfileSelector;
        [SerializeField]
        UITooltipDetector m_UGSSetupTooltipDetector;
        [SerializeField]
        Avatar m_DefaultCampaignAvatar;

        [Inject]
        AuthenticationServiceFacade m_AuthServiceFacade;
        [Inject]
        LocalSessionUser m_LocalUser;
        [Inject]
        LocalSession m_LocalSession;
        [Inject]
        ProfileManager m_ProfileManager;
        [Inject]
        ConnectionManager m_ConnectionManager;
        [Inject]
        DuelSessionState m_DuelSessionState;

        protected override void Awake()
        {
            base.Awake();

            ConfigureSoloVerticalSliceMenu();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_NameGenerationData);
            if (m_SessionUIMediator)
            {
                builder.RegisterComponent(m_SessionUIMediator);
            }

            if (m_IPUIMediator)
            {
                builder.RegisterComponent(m_IPUIMediator);
            }
        }

        async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions =
                    m_AuthServiceFacade.GenerateAuthenticationOptions(m_ProfileManager.Profile);

                await m_AuthServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                m_ProfileManager.onProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
            }
        }

        void OnAuthSignIn()
        {
            m_SessionButton.interactable = true;
            if (m_UGSSetupTooltipDetector)
            {
                m_UGSSetupTooltipDetector.enabled = false;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(false);
            }

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;

            // The local SessionUser object will be hooked into UI before the LocalSession is populated during session join, so the LocalSession must know about it already when that happens.
            m_LocalSession.AddUser(m_LocalUser);
        }

        void OnSignInFailed()
        {
            if (m_SessionButton)
            {
                m_SessionButton.interactable = true;
                m_UGSSetupTooltipDetector.enabled = false;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(false);
            }
        }

        void ConfigureSoloVerticalSliceMenu()
        {
            if (m_SessionUIMediator)
            {
                m_SessionUIMediator.Hide();
            }

            if (m_IPUIMediator)
            {
                m_IPUIMediator.Hide();
            }

            if (m_UIProfileSelector)
            {
                m_UIProfileSelector.Hide();
            }

            SetActiveIfFound("1v1 Start Button", false);
            SetActiveIfFound("Profile Button", false);
            SetActiveIfFound("SessionPopup", false);
            SetActiveIfFound("IPPopup", false);
            SetActiveIfFound("SignInSpinner", false);
            SetActiveIfFound("SettingsPanelCanvas", false);

            SetTextIfFound("Title", "ROGUE HERO 5");
            SetButtonText(m_SessionButton, "Start");

            m_SessionButton.interactable = true;
            if (m_UGSSetupTooltipDetector)
            {
                m_UGSSetupTooltipDetector.enabled = false;
            }
        }

        static void SetActiveIfFound(string objectName, bool active)
        {
            var gameObject = GameObject.Find(objectName);
            if (gameObject)
            {
                gameObject.SetActive(active);
            }
        }

        static void SetTextIfFound(string objectName, string text)
        {
            var gameObject = GameObject.Find(objectName);
            if (gameObject && gameObject.TryGetComponent(out TMP_Text label))
            {
                label.text = text;
            }
        }

        static void SetButtonText(Button button, string text)
        {
            if (!button)
            {
                return;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label)
            {
                label.text = text;
            }
        }

        protected override void OnDestroy()
        {
            if (m_ProfileManager != null)
            {
                m_ProfileManager.onProfileChanged -= OnProfileChanged;
            }

            base.OnDestroy();
        }

        async void OnProfileChanged()
        {
            if (m_SessionButton)
            {
                m_SessionButton.interactable = false;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(true);
            }

            await m_AuthServiceFacade.SwitchProfileAndReSignInAsync(m_ProfileManager.Profile);

            if (m_SessionButton)
            {
                m_SessionButton.interactable = true;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(false);
            }

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalSession
            m_LocalSession.RemoveUser(m_LocalUser);
            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            m_LocalSession.AddUser(m_LocalUser);
        }

        public void OnStartClicked()
        {
            if (m_SessionUIMediator)
            {
                m_SessionUIMediator.Hide();
            }

            if (m_IPUIMediator)
            {
                m_IPUIMediator.Hide();
            }

            m_DuelSessionState.StartCampaign(m_DefaultCampaignAvatar);
            m_ConnectionManager.MaxConnectedPlayers = DuelSessionState.CampaignMaxPlayers;
            m_ConnectionManager.StartHostIp(m_NameGenerationData.GenerateName(), IPUIMediator.k_DefaultIP, IPUIMediator.k_DefaultPort);
        }

        public void OnDirectIPClicked()
        {
            m_DuelSessionState.StartPvp();
            m_ConnectionManager.MaxConnectedPlayers = DuelSessionState.PvpMaxPlayers;
            if (m_SessionUIMediator)
            {
                m_SessionUIMediator.Hide();
            }

            if (m_IPUIMediator)
            {
                m_IPUIMediator.Show();
            }
        }

        public void OnChangeProfileClicked()
        {
            if (m_UIProfileSelector)
            {
                m_UIProfileSelector.Show();
            }
        }
    }
}
