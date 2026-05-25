using TMPro;
using Unity.BossRoom.Gameplay.GameState;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    public class DuelResultOverlay : MonoBehaviour
    {
        static DuelResultOverlay s_Instance;

        CanvasGroup m_CanvasGroup;
        TMP_Text m_Title;
        TMP_Text m_ButtonLabel;

        public static void Show(WinState winState)
        {
            EnsureInstance().ShowResult(winState);
        }

        public static void Hide()
        {
            if (s_Instance)
            {
                s_Instance.m_CanvasGroup.alpha = 0f;
                s_Instance.m_CanvasGroup.blocksRaycasts = false;
                s_Instance.m_CanvasGroup.interactable = false;
            }
        }

        static DuelResultOverlay EnsureInstance()
        {
            if (s_Instance)
            {
                return s_Instance;
            }

            EnsureEventSystem();

            var root = new GameObject("DuelResultOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(DuelResultOverlay));
            var rect = RequireComponent<RectTransform>(root);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = RequireComponent<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = RequireComponent<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            s_Instance = RequireComponent<DuelResultOverlay>(root);
            s_Instance.Build();
            Hide();
            return s_Instance;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>())
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        void Awake()
        {
            s_Instance = this;
            m_CanvasGroup = RequireComponent<CanvasGroup>(gameObject);
        }

        void Build()
        {
            var background = CreateImage("Background", transform, new Color(0f, 0f, 0f, 0.72f));
            Stretch(background.rectTransform);

            var panel = CreateImage("Panel", transform, new Color(0.08f, 0.075f, 0.07f, 0.96f));
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(620f, 260f);
            panel.rectTransform.anchoredPosition = Vector2.zero;

            m_Title = CreateText("Title", panel.transform, "Boss Defeated", 54f, FontStyles.Bold);
            m_Title.rectTransform.anchorMin = new Vector2(0.1f, 0.52f);
            m_Title.rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            m_Title.alignment = TextAlignmentOptions.Center;

            var buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = RequireComponent<RectTransform>(buttonObject);
            buttonRect.anchorMin = new Vector2(0.28f, 0.14f);
            buttonRect.anchorMax = new Vector2(0.72f, 0.38f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonImage = RequireComponent<Image>(buttonObject);
            buttonImage.color = new Color(0.83f, 0.66f, 0.32f, 1f);

            var button = RequireComponent<Button>(buttonObject);
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(ServerBossRoomState.RestartActiveVerticalSlice);

            m_ButtonLabel = CreateText("Text", buttonObject.transform, "Restart", 32f, FontStyles.Bold);
            Stretch(m_ButtonLabel.rectTransform);
            m_ButtonLabel.color = new Color(0.08f, 0.06f, 0.03f, 1f);
            m_ButtonLabel.alignment = TextAlignmentOptions.Center;
        }

        void ShowResult(WinState winState)
        {
            m_Title.text = winState == WinState.Win ? "Boss Defeated" : "You Fell";
            m_ButtonLabel.text = "Restart";
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
            m_CanvasGroup.interactable = true;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = RequireComponent<Image>(gameObject);
            image.color = color;
            return image;
        }

        static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var label = RequireComponent<TextMeshProUGUI>(gameObject);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = Color.white;
            label.enableWordWrapping = false;
            return label;
        }

        static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (!component)
            {
                throw new MissingComponentException($"{gameObject.name} requires {typeof(T).Name}.");
            }

            return component;
        }

        static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
