using System;
using Unity.BossRoom.Gameplay.GameState;
using Unity.BossRoom.Gameplay.GameplayObjects;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Action = Unity.BossRoom.Gameplay.Actions.Action;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Provides backing logic for all of the UI that runs in the PostGame stage.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField]
        private Light m_SceneLight;

        [SerializeField]
        private TextMeshProUGUI m_WinEndMessage;

        [SerializeField]
        private TextMeshProUGUI m_LoseGameMessage;

        [SerializeField]
        private GameObject m_ReplayButton;

        [SerializeField]
        private GameObject m_WaitOnHostMsg;

        [SerializeField]
        private Color m_WinLightColor;

        [SerializeField]
        private Color m_LoseLightColor;

        ServerPostGameState m_PostGameState;
        DuelSessionState m_DuelSessionState;
        GameObject m_CampaignRewardPanel;
        string m_DefaultWinMessage;
        string m_DefaultLoseMessage;

        [Inject]
        void Inject(ServerPostGameState postGameState, DuelSessionState duelSessionState)
        {
            m_PostGameState = postGameState;
            m_DuelSessionState = duelSessionState;

            // only hosts can restart the game, other players see a wait message
            if (NetworkManager.Singleton.IsHost)
            {
                m_ReplayButton.SetActive(true);
                m_WaitOnHostMsg.SetActive(false);
            }
            else
            {
                m_ReplayButton.SetActive(false);
                m_WaitOnHostMsg.SetActive(true);
            }
        }

        void Awake()
        {
            m_DefaultWinMessage = m_WinEndMessage.text;
            m_DefaultLoseMessage = m_LoseGameMessage.text;
        }

        void Start()
        {
            m_PostGameState.NetworkPostGame.WinState.OnValueChanged += OnWinStateChanged;
            SetPostGameUI(m_PostGameState.NetworkPostGame.WinState.Value);
        }

        void OnDestroy()
        {
            if (m_PostGameState != null)
            {
                m_PostGameState.NetworkPostGame.WinState.OnValueChanged -= OnWinStateChanged;
            }
        }

        void OnWinStateChanged(WinState previousValue, WinState newValue)
        {
            SetPostGameUI(newValue);
        }

        void SetPostGameUI(WinState winState)
        {
            switch (winState)
            {
                // Set end message and background color based last game outcome
                case WinState.Win:
                    m_SceneLight.color = m_WinLightColor;
                    m_WinEndMessage.text = m_DuelSessionState.IsCampaign
                        ? $"Boss Level {m_DuelSessionState.CampaignBossLevel} Cleared"
                        : m_DefaultWinMessage;
                    m_LoseGameMessage.text = m_DefaultLoseMessage;
                    m_WinEndMessage.gameObject.SetActive(true);
                    m_LoseGameMessage.gameObject.SetActive(false);
                    break;
                case WinState.Loss:
                    m_SceneLight.color = m_LoseLightColor;
                    m_WinEndMessage.text = m_DefaultWinMessage;
                    m_LoseGameMessage.text = m_DuelSessionState.IsCampaign ? "Campaign Failed" : m_DefaultLoseMessage;
                    m_WinEndMessage.gameObject.SetActive(false);
                    m_LoseGameMessage.gameObject.SetActive(true);
                    break;
                case WinState.DuelComplete:
                    m_SceneLight.color = m_WinLightColor;
                    m_WinEndMessage.text = "Duel Complete";
                    m_LoseGameMessage.text = m_DefaultLoseMessage;
                    m_WinEndMessage.gameObject.SetActive(true);
                    m_LoseGameMessage.gameObject.SetActive(false);
                    break;
                case WinState.Invalid:
                    Debug.LogWarning("PostGameUI encountered Invalid WinState");
                    break;
            }

            UpdateCampaignRewardUI(winState);
        }

        void UpdateCampaignRewardUI(WinState winState)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                return;
            }

            if (m_DuelSessionState.IsCampaign && winState == WinState.Win)
            {
                m_ReplayButton.SetActive(false);
                m_WaitOnHostMsg.SetActive(false);
                ShowCampaignRewardDraft();
                return;
            }

            DestroyCampaignRewardPanel();
            m_ReplayButton.SetActive(true);
            m_WaitOnHostMsg.SetActive(false);
        }

        void ShowCampaignRewardDraft()
        {
            DestroyCampaignRewardPanel();
            CreateCampaignRewardPanel("Choose an Ability");

            var draft = m_DuelSessionState.CreateCampaignRewardDraft(GameDataSource.Instance, null);
            foreach (var reward in draft)
            {
                var capturedReward = reward;
                AddRewardButton(DuelSessionState.GetActionDisplayName(reward), () => ShowSlotSelection(capturedReward));
            }

            AddRewardButton("Keep Current Loadout", ContinueCampaign);
        }

        void ShowSlotSelection(Action reward)
        {
            DestroyCampaignRewardPanel();
            CreateCampaignRewardPanel($"Equip {DuelSessionState.GetActionDisplayName(reward)}");

            for (int i = 0; i < DuelSessionState.LoadoutSlotCount; i++)
            {
                int capturedSlot = i;
                AddRewardButton($"Replace {m_DuelSessionState.GetCampaignLoadoutSlotName(i)}", () =>
                {
                    m_DuelSessionState.ApplyCampaignReward(reward, capturedSlot);
                    ContinueCampaign();
                });
            }

            AddRewardButton("Back", ShowCampaignRewardDraft);
        }

        void ContinueCampaign()
        {
            DestroyCampaignRewardPanel();
            m_PostGameState.ContinueCampaignAfterReward();
        }

        void CreateCampaignRewardPanel(string title)
        {
            var parent = m_ReplayButton.transform.parent ? m_ReplayButton.transform.parent : transform;
            m_CampaignRewardPanel = new GameObject("Campaign Reward Draft",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            m_CampaignRewardPanel.transform.SetParent(parent, false);

            var rectTransform = m_CampaignRewardPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -90f);
            rectTransform.sizeDelta = new Vector2(560f, 360f);

            var image = m_CampaignRewardPanel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.92f);

            var layout = m_CampaignRewardPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleObject.transform.SetParent(m_CampaignRewardPanel.transform, false);
            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.color = Color.white;
            titleText.fontSize = 28f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.raycastTarget = false;

            var titleLayout = titleObject.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 52f;
        }

        void AddRewardButton(string label, System.Action clicked)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(m_CampaignRewardPanel.transform, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.88f, 0.80f, 0.48f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => clicked());

            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.90f, 0.58f, 1f);
            colors.pressedColor = new Color(0.70f, 0.62f, 0.34f, 1f);
            button.colors = colors;

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 54f;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.color = new Color(0.07f, 0.06f, 0.04f, 1f);
            text.fontSize = 21f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        void DestroyCampaignRewardPanel()
        {
            if (m_CampaignRewardPanel)
            {
                m_CampaignRewardPanel.SetActive(false);
                Destroy(m_CampaignRewardPanel);
                m_CampaignRewardPanel = null;
            }
        }

        public void OnPlayAgainClicked()
        {
            m_PostGameState.PlayAgain();
        }

        public void OnMainMenuClicked()
        {
            m_PostGameState.GoToMainMenu();
        }
    }
}

