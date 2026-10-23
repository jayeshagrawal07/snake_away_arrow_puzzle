using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Displays completion modal when all snakes have successfully escaped.
    /// Automatically builds clean UI if references are not pre-assigned in scene.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        public event Action OnNextLevelClicked;
        public event Action OnReplayClicked;

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;

        private void Awake()
        {
            EnsureUI();
        }

        public void Bind(Action onNext, Action onReplay)
        {
            EnsureUI();

            OnNextLevelClicked = onNext;
            OnReplayClicked = onReplay;

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveAllListeners();
                replayButton.onClick.AddListener(() => OnReplayClicked?.Invoke());
            }

            Hide();
        }

        public void Show(string levelId)
        {
            EnsureUI();

            if (modalRoot != null)
                modalRoot.SetActive(true);

            if (titleText != null)
                titleText.text = $"Level Cleared!";
        }

        public void Hide()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void EnsureUI()
        {
            if (modalRoot != null) return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Fullscreen Modal Root / Backdrop
            modalRoot = new GameObject("LevelCompleteModal", typeof(RectTransform), typeof(Image));
            modalRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = modalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image backdrop = modalRoot.GetComponent<Image>();
            backdrop.color = new Color(0.04f, 0.07f, 0.14f, 0.72f); // Dark blur-like overlay

            // Card Panel
            GameObject cardObj = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(modalRoot.transform, false);

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(760f, 620f);

            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = Color.white;

            // Celebration Icon / Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -80f);
            titleRt.sizeDelta = new Vector2(-60f, 120f);

            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Level Cleared!";
            titleText.fontSize = 62;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.12f, 0.44f, 0.98f); // Royal Blue
            titleText.alignment = TextAlignmentOptions.Center;

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform));
            subObj.transform.SetParent(cardObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -190f);
            subRt.sizeDelta = new Vector2(-60f, 80f);

            TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "All snakes untangled!";
            subText.fontSize = 38;
            subText.color = new Color(0.40f, 0.46f, 0.56f);
            subText.alignment = TextAlignmentOptions.Center;

            // Next Level Button
            nextLevelButton = CreateCardButton(cardObj.transform, "NextBtn", "Next Level ▶", new Vector2(0f, -340f),
                                               new Color(0.12f, 0.44f, 0.98f), Color.white);

            // Replay Button
            replayButton = CreateCardButton(cardObj.transform, "ReplayBtn", "↺ Play Again", new Vector2(0f, -470f),
                                            new Color(0.92f, 0.94f, 0.98f), new Color(0.22f, 0.28f, 0.40f));
        }

        private static Button CreateCardButton(Transform parent, string name, string label, Vector2 pos, Color bgColor, Color textColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(560f, 100f);

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            GameObject textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 40;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj.GetComponent<Button>();
        }
    }
}
